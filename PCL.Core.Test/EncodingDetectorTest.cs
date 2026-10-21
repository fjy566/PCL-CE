using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Utils.Codecs;

namespace PCL.Core.Test;
[TestClass]
public class EncodingDetectorTest
{
    [TestMethod]
    [DataRow("utf-8")]
    [DataRow("utf-16")]
    [DataRow("utf-16BE")]
    [DataRow("utf-32")]
    [DataRow("utf-32BE")]
    public void DetectBomWithContentAndRestoreStreamPosition(string name)
    {
        var encoding = Encoding.GetEncoding(name);
        using var stream = new MemoryStream();
        stream.Write(encoding.GetPreamble());
        stream.Write(encoding.GetBytes("你好 world"));
        stream.Position = stream.Length;
        Assert.AreEqual(encoding.CodePage, EncodingDetector.DetectEncoding(stream, readFromBegin: true).CodePage);
        Assert.AreEqual(stream.Length, stream.Position);
    }

    [TestMethod]
    public void DetectUtf8AcrossSampleBoundary()
    {
        var bytes = Encoding.UTF8.GetBytes(new string('a', 1023) + "你好");
        Assert.AreEqual(Encoding.UTF8, EncodingDetector.DetectEncoding(bytes));
    }

    [TestMethod]
    public void TestEncoding()
    {
        var utf8 = Encoding.UTF8.GetBytes("Hi, There!");
        Assert.AreEqual(EncodingDetector.DetectEncoding(utf8), Encoding.UTF8);
        utf8 = Encoding.UTF8.GetBytes("棍斤拷烫烫烫");
        Assert.AreEqual(EncodingDetector.DetectEncoding(utf8), Encoding.UTF8);
        var gb = Encoding.GetEncoding("gb2312").GetBytes("你好世界");
        Assert.AreEqual(EncodingDetector.DetectEncoding(gb), Encoding.GetEncoding("gb2312"));
        // var gbnew = Encoding.GetEncoding("GB18030").GetBytes("你好世界");
        // Assert.AreEqual(EncodingDetector.DetectEncoding(gbnew), Encoding.GetEncoding("GB18030"));
        byte[] nonEncode = [0xfe, 0x5f, 0xa1];
        Assert.AreEqual(Encoding.Default, EncodingDetector.DetectEncoding(nonEncode));
    }
}
