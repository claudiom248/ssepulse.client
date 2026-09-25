using Microsoft.Extensions.Time.Testing;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests;

public sealed class FileLastEventIdStoreTests : IDisposable
{
    private readonly string _tempDir;

    public FileLastEventIdStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SsePulse_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string TempFile(string name = "last-event-id.txt") => Path.Combine(_tempDir, name);

    [Fact]
    public void Constructor_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FileLastEventIdStore(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenFilePathIsNullOrWhitespace_ThrowsArgumentException(string filePath)
    {
        FileLastEventIdStoreOptions options = new() { FilePath = filePath };

        Assert.Throws<ArgumentException>(() => new FileLastEventIdStore(options));
    }

    [Fact]
    public void Constructor_WhenFlushAfterCountIsZero_ThrowsArgumentException()
    {
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = TempFile(),
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 0
        };

        Assert.Throws<ArgumentException>(() => new FileLastEventIdStore(options));
    }

    [Fact]
    public void Constructor_WhenFlushIntervalIsZero_ThrowsArgumentException()
    {
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = TempFile(),
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.Zero
        };

        Assert.Throws<ArgumentException>(() => new FileLastEventIdStore(options));
    }

    [Fact]
    public async Task Get_WhenFileDoesNotExist_ReturnsNull()
    {
        FileLastEventIdStoreOptions options = new() { FilePath = TempFile() };
        await using FileLastEventIdStore store = new(options);

        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_WhenFileExists_ReadsLastEventId()
    {
        string path = TempFile();
        await File.WriteAllTextAsync(path, "event-id-from-previous-session");
        await using FileLastEventIdStore store = new(new FileLastEventIdStoreOptions { FilePath = path });

        Assert.Equal("event-id-from-previous-session", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Constructor_DoesNotReadTheFile()
    {
        string path = TempFile();
        await File.WriteAllTextAsync(path, "before-construction");
        await using FileLastEventIdStore store = new(new FileLastEventIdStoreOptions { FilePath = path });
        await File.WriteAllTextAsync(path, "after-construction");

        Assert.Equal("after-construction", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_AfterASetBeforeTheFirstRead_KeepsTheValueSetInMemory()
    {
        string path = TempFile();
        await File.WriteAllTextAsync(path, "persisted");
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.FromMinutes(5)
        };
        await using FileLastEventIdStore store = new(options, timeProvider: new FakeTimeProvider());

        await store.SetLastEventIdAsync("in-memory");

        Assert.Equal("in-memory", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithEverySet_WritesEventIdToFile()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.EverySet
        };
        await using FileLastEventIdStore store = new(options);

        await store.SetLastEventIdAsync("event-42");

        Assert.Equal("event-42", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Set_WithEverySet_MultipleIds_FileContainsLatestId()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.EverySet
        };
        await using FileLastEventIdStore store = new(options);

        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");
        await store.SetLastEventIdAsync("event-3");

        Assert.Equal("event-3", await File.ReadAllTextAsync(path));
        Assert.Equal("event-3", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithAfterCount_DoesNotWriteBeforeThreshold()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 5
        };
        await using FileLastEventIdStore store = new(options);

        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");
        await store.SetLastEventIdAsync("event-3");
        await store.SetLastEventIdAsync("event-4");

        Assert.False(File.Exists(path));
        Assert.Equal("event-4", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithAfterCount_WritesAtThreshold()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 3
        };
        await using FileLastEventIdStore store = new(options);

        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");
        await store.SetLastEventIdAsync("event-3");

        Assert.True(File.Exists(path));
        Assert.Equal("event-3", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Set_WithAfterInterval_DoesNotWriteImmediately()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.FromSeconds(30)
        };
        FakeTimeProvider time = new();
        await using FileLastEventIdStore store = new(options, timeProvider: time);

        await store.SetLastEventIdAsync("event-1");
        time.Advance(TimeSpan.FromSeconds(29));

        Assert.False(File.Exists(path));
        Assert.Equal("event-1", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithAfterInterval_WritesAfterIntervalElapses()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.FromMilliseconds(100)
        };
        FakeTimeProvider time = new();
        await using FileLastEventIdStore store = new(options, timeProvider: time);

        await store.SetLastEventIdAsync("event-interval");
        time.Advance(TimeSpan.FromMilliseconds(100));
        await TestWait.UntilAsync(() => Task.FromResult(File.Exists(path)));

        Assert.Equal("event-interval", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task DisposeAsync_WithAfterInterval_FlushesPendingWrite()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.FromSeconds(30)
        };
        FileLastEventIdStore store = new(options);
        await store.SetLastEventIdAsync("event-on-dispose");

        await store.DisposeAsync();

        Assert.True(File.Exists(path));
        Assert.Equal("event-on-dispose", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Dispose_WithAfterCount_FlushesPendingWrite()
    {
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 10
        };
        FileLastEventIdStore store = new(options);
        await store.SetLastEventIdAsync("event-on-dispose");

        store.Dispose();

        Assert.True(File.Exists(path));
        Assert.Equal("event-on-dispose", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task Get_AfterRestart_ReturnsPersistedValue()
    {
        string path = TempFile();
        await using (FileLastEventIdStore first = new(new FileLastEventIdStoreOptions { FilePath = path }))
        {
            await first.SetLastEventIdAsync("session-1-last-event");
        }

        await using FileLastEventIdStore second = new(new FileLastEventIdStoreOptions { FilePath = path });

        Assert.Equal("session-1-last-event", await second.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_AfterAFailedWrite_PersistsTheNewestValueOnTheNextSet()
    {
        string directory = Path.Combine(_tempDir, "not-yet-created");
        string path = Path.Combine(directory, "last-event-id.txt");
        await using FileLastEventIdStore store = new(new FileLastEventIdStoreOptions { FilePath = path });
        await store.SetLastEventIdAsync("event-1");
        Directory.CreateDirectory(directory);

        await store.SetLastEventIdAsync("event-2");

        Assert.Equal("event-2", await File.ReadAllTextAsync(path));
    }
}
