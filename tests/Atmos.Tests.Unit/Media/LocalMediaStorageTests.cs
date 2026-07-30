using System.Text;
using Atmos.Services.Media.Options;
using Atmos.Services.Media.Providers;

namespace Atmos.Tests.Unit.Media;

public class LocalMediaStorageTests
{
    /// <summary>
    /// Runs the body against a storage instance rooted in a fresh temp directory,
    /// then removes the whole sandbox whatever happens.
    ///
    /// The media root lives one level below a unique per-test sandbox parent
    /// (rather than being the sandbox itself) so that a path-traversal escape
    /// target — e.g. "../escape.png" resolving to the sandbox parent — is also
    /// unique per test. Tests run in parallel, so a shared path like
    /// Path.GetTempPath() itself would make an "escape" assertion racy.
    /// </summary>
    private static async Task WithStorage(Func<LocalMediaStorage, string, string, Task> body)
    {
        var sandbox = Path.Combine(Path.GetTempPath(), $"atmos-media-{Path.GetRandomFileName()}");
        var root = Path.Combine(sandbox, "root");
        Directory.CreateDirectory(root);

        var storage = new LocalMediaStorage(Microsoft.Extensions.Options.Options.Create(new MediaOptions
        {
            Local = new LocalStorageOptions
            {
                RootPath = root
            }
        }));

        try
        {
            await body(storage, root, sandbox);
        }
        finally
        {
            Directory.Delete(sandbox, true);
        }
    }

    private static async Task UploadTextAsync(LocalMediaStorage storage, string key, string text)
    {
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await storage.UploadAsync(key, content, "text/plain");
    }

    [Test]
    public async Task Uploaded_Object_Exists_And_Content_Round_Trips()
    {
        await WithStorage(async (storage, root, _) =>
        {
            await UploadTextAsync(storage, "hello.txt", "media payload");

            await Assert.That(await storage.ExistsAsync("hello.txt")).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(Path.Combine(root, "hello.txt")))
                .IsEqualTo("media payload");
        });
    }

    [Test]
    public async Task Upload_Creates_Nested_Directories_For_Prefixed_Key()
    {
        await WithStorage(async (storage, root, _) =>
        {
            await UploadTextAsync(storage, "blog/2026/photo.webp", "bytes");

            await Assert.That(File.Exists(Path.Combine(root, "blog", "2026", "photo.webp"))).IsTrue();
        });
    }

    [Test]
    public async Task Upload_Leaves_No_Temp_Files_Behind()
    {
        await WithStorage(async (storage, root, _) =>
        {
            await UploadTextAsync(storage, "blog/photo.webp", "bytes");

            var strays = Directory.GetFiles(root, "*.tmp-*", SearchOption.AllDirectories);
            await Assert.That(strays).IsEmpty();
        });
    }

    [Test]
    public async Task Upload_With_Existing_Key_Overwrites_In_Place()
    {
        await WithStorage(async (storage, root, _) =>
        {
            await UploadTextAsync(storage, "hello.txt", "first");
            await UploadTextAsync(storage, "hello.txt", "second");

            await Assert.That(await File.ReadAllTextAsync(Path.Combine(root, "hello.txt")))
                .IsEqualTo("second");
            await Assert.That(Directory.GetFiles(root)).Count().IsEqualTo(1);
        });
    }

    [Test]
    public async Task DeleteAsync_Removes_The_Object()
    {
        await WithStorage(async (storage, _, _) =>
        {
            await UploadTextAsync(storage, "blog/photo.webp", "bytes");

            await storage.DeleteAsync("blog/photo.webp");

            await Assert.That(await storage.ExistsAsync("blog/photo.webp")).IsFalse();
        });
    }

    [Test]
    public async Task DeleteAsync_Is_Idempotent_For_Missing_Objects()
    {
        await WithStorage(async (storage, _, _) =>
        {
            // Nested key whose prefix directory does not exist either: File.Delete
            // throws DirectoryNotFoundException in that case if unguarded
            await storage.DeleteAsync("never/written/at-all.png");

            await Assert.That(await storage.ExistsAsync("never/written/at-all.png")).IsFalse();
        });
    }

    [Test]
    public async Task ExistsAsync_Is_False_For_Absent_Object()
    {
        await WithStorage(async (storage, _, _) =>
        {
            await Assert.That(await storage.ExistsAsync("nothing-here.png")).IsFalse();
        });
    }

    [Test]
    [Arguments("../escape.png")]
    [Arguments("a/../../escape.png")]
    [Arguments("/etc/passwd")]
    [Arguments("a//b.png")]
    [Arguments("")]
    public async Task Invalid_Keys_Throw_And_Write_Nothing(string key)
    {
        await WithStorage(async (storage, root, sandbox) =>
        {
            await Assert.That(async () => await UploadTextAsync(storage, key, "payload"))
                .Throws<ArgumentException>();

            await Assert.That(Directory.GetFileSystemEntries(root)).IsEmpty();

            // Guards against a regression where traversal escapes the root: e.g.
            // "a/../../escape.png" resolves one level above root, inside the
            // per-test sandbox parent, which this checks contains nothing but
            // the (empty) root directory itself.
            await Assert.That(Directory.GetFileSystemEntries(sandbox)).IsEquivalentTo(new[] { root });
        });
    }

    // ResolvePath is the sole security control on all three IMediaStorage
    // methods, not just UploadAsync. MediaEndpoints.DeleteMedia in particular
    // passes media.Key straight from the database without re-running
    // MediaKeyUtils.IsValid, so this is the only test standing behind that path.
    [Test]
    [Arguments("../escape.png")]
    [Arguments("a/../../escape.png")]
    [Arguments("/etc/passwd")]
    [Arguments("a//b.png")]
    [Arguments("")]
    public async Task Invalid_Keys_Throw_From_DeleteAsync(string key)
    {
        await WithStorage(async (storage, _, _) =>
        {
            await Assert.That(async () => await storage.DeleteAsync(key))
                .Throws<ArgumentException>();
        });
    }

    [Test]
    [Arguments("../escape.png")]
    [Arguments("a/../../escape.png")]
    [Arguments("/etc/passwd")]
    [Arguments("a//b.png")]
    [Arguments("")]
    public async Task Invalid_Keys_Throw_From_ExistsAsync(string key)
    {
        await WithStorage(async (storage, _, _) =>
        {
            await Assert.That(async () => await storage.ExistsAsync(key))
                .Throws<ArgumentException>();
        });
    }

    [Test]
    public async Task Upload_Failure_Mid_Copy_Leaves_No_Partial_Or_Temp_File_And_Preserves_Existing_Content()
    {
        await WithStorage(async (storage, root, _) =>
        {
            // Fresh key: a stream that fails partway through the copy must leave
            // nothing at the final path.
            await using (var failingStream = new FailingAfterBytesStream(Encoding.UTF8.GetBytes("partial")))
            {
                await Assert.That(async () => await storage.UploadAsync("fresh-key.png", failingStream, "image/png"))
                    .Throws<IOException>();
            }

            await Assert.That(File.Exists(Path.Combine(root, "fresh-key.png"))).IsFalse();

            // Existing key: a failed re-upload must leave the original file intact.
            await UploadTextAsync(storage, "existing.png", "original content");

            await using (var failingStream = new FailingAfterBytesStream(Encoding.UTF8.GetBytes("partial")))
            {
                await Assert.That(async () => await storage.UploadAsync("existing.png", failingStream, "image/png"))
                    .Throws<IOException>();
            }

            await Assert.That(await File.ReadAllTextAsync(Path.Combine(root, "existing.png")))
                .IsEqualTo("original content");

            await Assert.That(Directory.GetFiles(root, "*.tmp-*", SearchOption.AllDirectories)).IsEmpty();
        });
    }

    /// <summary>
    /// Yields a fixed prefix of bytes and then throws <see cref="IOException" />,
    /// simulating a source stream (e.g. the request body) failing partway through
    /// <c>CopyToAsync</c> - a disk-full or client-disconnect scenario.
    /// </summary>
    private sealed class FailingAfterBytesStream : Stream
    {
        private readonly byte[] _bytes;
        private int _position;

        public FailingAfterBytesStream(byte[] bytes)
        {
            _bytes = bytes;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _bytes.Length)
            {
                throw new IOException("Simulated failure partway through the copy");
            }

            var toCopy = Math.Min(count, _bytes.Length - _position);
            Array.Copy(_bytes, _position, buffer, offset, toCopy);
            _position += toCopy;

            return toCopy;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
