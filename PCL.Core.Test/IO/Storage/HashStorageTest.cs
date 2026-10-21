using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.IO.Storage;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Hash;

namespace PCL.Core.Test.IO.Storage;

[TestClass]
public class HashStorageTest
{
    [TestMethod]
    public async Task ConcurrentWritersPublishOneCompleteObject()
    {
        var root = Path.Combine(Path.GetTempPath(), "PCLTest", "HashStorage", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new HashStorage(root, SHA256Provider.Instance, compressObjects: true);
            var content = new byte[1_000_000];
            new Random(42).NextBytes(content);
            var hashes = await Task.WhenAll(Enumerable.Range(0, 16).Select(async _ =>
            {
                using var input = new MemoryStream(content);
                var hash = await storage.PutAsync(input);
                using var retrieved = storage.Get(hash!);
                Assert.IsNotNull(retrieved);
                using var output = new MemoryStream();
                await retrieved.CopyToAsync(output);
                CollectionAssert.AreEqual(content, output.ToArray());
                return hash;
            }));
            Assert.AreEqual(1, hashes.Distinct().Count());
            Assert.AreEqual(1, Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(true, false, false)]
    [DataRow(false, true, false)]
    [DataRow(true, true, false)]
    [DataRow(false, false, true)]
    [DataRow(true, false, true)]
    [DataRow(false, true, true)]
    [DataRow(true, true, true)]
    public async Task StoreAndRetrievePreserveContent(bool compressed, bool nonSeekable, bool knownHash)
    {
        var root = Path.Combine(Path.GetTempPath(), "PCLTest", "HashStorage", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new HashStorage(root, SHA256Provider.Instance, compressObjects: compressed);
            var content = new byte[100_000];
            new Random(42).NextBytes(content);
            var expectedHash = SHA256Provider.Instance.ComputeHash(content).ToHexString();
            using Stream input = nonSeekable ? new NonSeekableStream(content) : new MemoryStream(content);
            var hash = await storage.PutAsync(input, knownHash ? expectedHash : null);
            Assert.AreEqual(expectedHash, hash);
            Assert.IsTrue(input.CanRead, "Storage must not dispose the caller's stream.");
            using var retrieved = storage.Get(hash!);
            Assert.IsNotNull(retrieved);
            using var output = new MemoryStream();
            await retrieved.CopyToAsync(output);
            CollectionAssert.AreEqual(content, output.ToArray());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private sealed class NonSeekableStream(byte[] content) : MemoryStream(content)
    {
        public override bool CanSeek => false;
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }
}
