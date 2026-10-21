using System;
using System.IO;
using System.Text;

namespace PCL.Core.Utils.Codecs;

public static class EncodingDetector
{
    static EncodingDetector() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>
    /// 检测流中的文本编码方式（支持 Seek 的流）
    /// </summary>
    /// <param name="stream">输入流，必须支持 Seek</param>
    /// <param name="readFromBegin">是否将流重置到起始点</param>
    /// <returns>检测到的编码，未识别时返回 UTF-8 或系统默认</returns>
    public static Encoding DetectEncoding(Stream stream, bool readFromBegin = false)
    {
        if (!stream.CanRead)
            throw new ArgumentException("流必须支持读操作");
        if (!stream.CanSeek)
            throw new ArgumentException("流必须支持 Seek 操作");

        var originalPosition = stream.Position;
        var samplePosition = readFromBegin ? 0 : originalPosition;

        try
        {
            return _DetectByBom(stream, samplePosition) ?? _DetectWithoutBOM(stream, samplePosition) ?? Encoding.Default;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    public static Encoding DetectEncoding(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return DetectEncoding(stream, true);
    }

    /// <summary>
    /// 根据 BOM 判断编码
    /// </summary>
    private static Encoding? _DetectByBom(Stream stream, long originalPosition)
    {
        stream.Position = originalPosition;
        // 获取最长样本长度
        var readableLength = stream.Length - stream.Position;
        var sampleLength = Math.Min(readableLength, 4);
        var buffer = new byte[sampleLength];
        stream.ReadExactly(buffer);

        // 对样本进行分析
        if (buffer is [0xef, 0xbb, 0xbf, ..])
            return Encoding.UTF8; // UTF-8

        if (sampleLength >= 2)
        {
            if (buffer is [0xfe, 0xff, ..])
                return Encoding.BigEndianUnicode; // UTF-16 BE
            if (buffer is [0xff, 0xfe, ..])
            {
                if (sampleLength >= 4 && buffer is [_, _, 0x00, 0x00])
                    return Encoding.UTF32; // UTF-32 LE
                return Encoding.Unicode;   // UTF-16 LE
            }
        }

        if (sampleLength >= 4)
        {
            if (buffer is [0x00, 0x00, 0xfe, 0xff])
                return Encoding.GetEncoding("utf-32BE"); // UTF-32 BE
            if (buffer is [0xff, 0xfe, 0x00, 0x00])
                return Encoding.UTF32; // UTF-32 LE
        }

        return null;
    }

    /// <summary>
    /// BOM 不存在时的备用检测策略
    /// </summary>
    private static Encoding? _DetectWithoutBOM(Stream stream, long originalPosition)
    {
        // 尝试验证是否为有效 UTF-8
        if (_IsValidUtf8(stream, originalPosition)) return Encoding.UTF8;

        // Chinese crash logs may use the system's legacy GB2312/GBK code page.
        stream.Position = originalPosition;
        var buffer = new byte[(int)Math.Min(stream.Length - originalPosition, 1024)];
        stream.ReadExactly(buffer);
        try
        {
            var encoding = Encoding.GetEncoding("gb2312", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            encoding.GetDecoder().GetCharCount(buffer, 0, buffer.Length, flush: stream.Position == stream.Length);
            return Encoding.GetEncoding("gb2312");
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    /// <summary>
    /// 验证流样本是否为合法 UTF-8，允许样本边界处出现未完成的多字节字符。
    /// </summary>
    private static bool _IsValidUtf8(Stream stream, long originalPosition)
    {
        const int sampleSize = 1024;
        var buffer = new byte[(int)Math.Min(stream.Length - originalPosition, sampleSize)];
        stream.Position = originalPosition;
        stream.ReadExactly(buffer);

        try
        {
            var decoder = new UTF8Encoding(false, true).GetDecoder();
            decoder.GetCharCount(buffer, 0, buffer.Length, flush: stream.Position == stream.Length);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
