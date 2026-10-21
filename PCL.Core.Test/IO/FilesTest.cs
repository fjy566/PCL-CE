using System;
using System.IO;
using System.IO.Compression;
using System.Formats.Tar;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.IO;

namespace PCL.Core.Test.IO;

[TestClass]
public class FilesTest
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PCLTest", "Files", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, true);

    [TestMethod]
    public async Task AppendTextCreatesMissingFileAndPreservesExistingEncoding()
    {
        var path = Path.Combine(_root, "new", "log.txt");
        await Files.WriteFileAsync(path, "first", append: true);
        await Files.WriteFileAsync(path, " second", append: true);
        Assert.AreEqual("first second", await File.ReadAllTextAsync(path));

        await File.WriteAllTextAsync(path, "原文", Encoding.Unicode);
        await Files.WriteFileAsync(path, "追加", append: true);
        Assert.AreEqual("原文追加", await File.ReadAllTextAsync(path, Encoding.Unicode));
    }

    [TestMethod]
    [DataRow(".tar")]
    [DataRow(".tar.gz")]
    [DataRow(".tgz")]
    public async Task ExtractTarPreservesEveryEntry(string extension)
    {
        var archive = Path.Combine(_root, "world" + extension);
        await using (var file = File.Create(archive))
        {
            await using var compressed = extension == ".tar" ? null : new GZipStream(file, CompressionMode.Compress, leaveOpen: true);
            using var writer = new TarWriter(compressed ?? (Stream)file, TarEntryFormat.Pax, leaveOpen: true);
            writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "./"));
            writer.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "world/"));
            writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "world/level.dat")
            {
                DataStream = new MemoryStream("world data"u8.ToArray())
            });
            writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "world/empty.txt")
            {
                DataStream = new MemoryStream()
            });
            writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "readme.txt")
            {
                DataStream = new MemoryStream("second file"u8.ToArray())
            });
        }

        var destination = Path.Combine(_root, "extracted");
        double lastProgress = 0;
        await Files.ExtractFileAsync(archive, destination, value => lastProgress = value);

        Assert.AreEqual("world data", await File.ReadAllTextAsync(Path.Combine(destination, "world", "level.dat")));
        Assert.AreEqual("second file", await File.ReadAllTextAsync(Path.Combine(destination, "readme.txt")));
        Assert.AreEqual(0L, new FileInfo(Path.Combine(destination, "world", "empty.txt")).Length);
        Assert.AreEqual(1.0, lastProgress);
    }

    [TestMethod]
    public async Task ExtractZipWithDirectoryEntriesCompletesProgress()
    {
        var archive = Path.Combine(_root, "world.zip");
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
        {
            zip.CreateEntry("world/");
            using var writer = new StreamWriter(zip.CreateEntry("world/level.dat").Open());
            writer.Write("world data");
        }
        double lastProgress = 0;
        await Files.ExtractFileAsync(archive, Path.Combine(_root, "extracted"), value => lastProgress = value);
        Assert.AreEqual(1.0, lastProgress);
    }

    [TestMethod]
    public async Task ExtractCancelledZipDoesNotCreateEntries()
    {
        var archive = Path.Combine(_root, "world.zip");
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
            zip.CreateEntry("empty.txt");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var destination = Path.Combine(_root, "extracted");
        await Assert.ThrowsAsync<OperationCanceledException>(() => Files.ExtractFileAsync(archive, destination,
            cancellationToken: cancellation.Token));
        Assert.IsFalse(File.Exists(Path.Combine(destination, "empty.txt")));
    }

    [TestMethod]
    [DataRow(".tar")]
    [DataRow(".zip")]
    public async Task ExtractArchiveRejectsPathTraversal(string extension)
    {
        var archive = Path.Combine(_root, "unsafe" + extension);
        if (extension == ".tar")
        {
            using var file = File.Create(archive);
            using var writer = new TarWriter(file);
            writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "../outside.txt")
            {
                DataStream = new MemoryStream("must stay inside"u8.ToArray())
            });
        }
        else
        {
            using var zip = ZipFile.Open(archive, ZipArchiveMode.Create);
            zip.CreateEntry("../outside.txt");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Files.ExtractFileAsync(archive, Path.Combine(_root, "extracted")));
        Assert.IsFalse(File.Exists(Path.Combine(_root, "outside.txt")));
    }
}
