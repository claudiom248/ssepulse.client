using SsePulse.Client.Core;
using SsePulse.Client.Core.Configurations;

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
        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() => new FileLastEventIdStore(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenFilePathIsNullOrWhitespace_ThrowsArgumentException(string filePath)
    {
        // ARRANGE
        FileLastEventIdStoreOptions options = new() { FilePath = filePath };

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => new FileLastEventIdStore(options));
    }

    [Fact]
    public void Constructor_WhenFlushAfterCountIsZero_ThrowsArgumentException()
    {
        // ARRANGE
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = TempFile(),
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 0
        };

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => new FileLastEventIdStore(options));
    }

    [Fact]
    public void Constructor_WhenFlushIntervalIsZero_ThrowsArgumentException()
    {
        // ARRANGE
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = TempFile(),
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.Zero
        };

        // ACT & ASSERT
        Assert.Throws<ArgumentException>(() => new FileLastEventIdStore(options));
    }

    [Fact]
    public void Constructor_WhenFileDoesNotExist_LastEventIdIsNull()
    {
        // ARRANGE
        FileLastEventIdStoreOptions options = new() { FilePath = TempFile() };

        // ACT
        using FileLastEventIdStore store = new(options);

        // ASSERT
        Assert.Null(store.LastEventId);
    }

    [Fact]
    public void Constructor_WhenFileExists_ReadsLastEventId()
    {
        // ARRANGE
        string path = TempFile();
        File.WriteAllText(path, "event-id-from-previous-session");
        FileLastEventIdStoreOptions options = new() { FilePath = path };

        // ACT
        using FileLastEventIdStore store = new(options);

        // ASSERT
        Assert.Equal("event-id-from-previous-session", store.LastEventId);
    }
    
    [Fact]
    public void Set_WithEverySet_WritesEventIdToFile()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.EverySet
        };
        using FileLastEventIdStore store = new(options);

        // ACT
        store.Set("event-42");

        // ASSERT
        Assert.Equal("event-42", File.ReadAllText(path));
    }

    [Fact]
    public void Set_WithEverySet_MultipleIds_FileContainsLatestId()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.EverySet
        };
        using FileLastEventIdStore store = new(options);

        // ACT
        store.Set("event-1");
        store.Set("event-2");
        store.Set("event-3");

        // ASSERT
        Assert.Equal("event-3", File.ReadAllText(path));
        Assert.Equal("event-3", store.LastEventId);
    }
    
    [Fact]
    public void Set_WithAfterCount_DoesNotWriteBeforeThreshold()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 5
        };
        using FileLastEventIdStore store = new(options);

        // ACT — call Set 4 times (below threshold of 5)
        store.Set("event-1");
        store.Set("event-2");
        store.Set("event-3");
        store.Set("event-4");

        // ASSERT — file must not have been written yet
        Assert.False(File.Exists(path));
        Assert.Equal("event-4", store.LastEventId);
    }

    [Fact]
    public void Set_WithAfterCount_WritesAtThreshold()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 3
        };
        using FileLastEventIdStore store = new(options);

        // ACT
        store.Set("event-1");
        store.Set("event-2");
        store.Set("event-3"); // 3rd call — flush

        // ASSERT
        Assert.True(File.Exists(path));
        Assert.Equal("event-3", File.ReadAllText(path));
    }
    
    [Fact]
    public async Task Set_WithAfterInterval_DoesNotWriteImmediately()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.FromSeconds(30)
        };
        using FileLastEventIdStore store = new(options);

        // ACT
        store.Set("event-1");
        await Task.Delay(50); // well within the 30-second window

        // ASSERT — file must not have been written yet
        Assert.False(File.Exists(path));
        Assert.Equal("event-1", store.LastEventId);
    }

    [Fact]
    public async Task Set_WithAfterInterval_WritesAfterIntervalElapses()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.FromMilliseconds(100)
        };
        using FileLastEventIdStore store = new(options);

        // ACT
        store.Set("event-interval");
        await Task.Delay(400); // wait well past the 100 ms interval

        // ASSERT
        Assert.True(File.Exists(path));
        Assert.Equal("event-interval", File.ReadAllText(path));
    }
    
    [Fact]
    public void Dispose_WithAfterInterval_FlushesPendingWrite()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterInterval,
            FlushInterval = TimeSpan.FromSeconds(30)
        };
        FileLastEventIdStore store = new(options);
        store.Set("event-on-dispose");

        // ACT
        store.Dispose();

        // ASSERT
        Assert.True(File.Exists(path));
        Assert.Equal("event-on-dispose", File.ReadAllText(path));
    }

    [Fact]
    public void Dispose_WithAfterCount_FlushesPendingWrite()
    {
        // ARRANGE
        string path = TempFile();
        FileLastEventIdStoreOptions options = new()
        {
            FilePath = path,
            FlushMode = FlushMode.AfterCount,
            FlushAfterCount = 10
        };
        FileLastEventIdStore store = new(options);
        store.Set("event-on-dispose");

        // ACT
        store.Dispose();

        // ASSERT 
        Assert.True(File.Exists(path));
        Assert.Equal("event-on-dispose", File.ReadAllText(path));
    }
    
    [Fact]
    public void LastEventId_AfterRestart_ReturnsPersistedValue()
    {
        // ARRANGE
        string path = TempFile();
        using (FileLastEventIdStore first = new(new FileLastEventIdStoreOptions { FilePath = path }))
        {
            first.Set("session-1-last-event");
        }

        // ACT — "restart": create a new store pointing at the same file
        using FileLastEventIdStore second = new(new FileLastEventIdStoreOptions { FilePath = path });

        // ASSERT
        Assert.Equal("session-1-last-event", second.LastEventId);
    }
}

