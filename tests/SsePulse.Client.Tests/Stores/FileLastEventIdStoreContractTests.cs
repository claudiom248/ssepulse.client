using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Stores;

public sealed class FileLastEventIdStoreContractTests : DurableLastEventIdStoreContract, IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("sse-contract-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    protected override ILastEventIdStore CreateStore() => CreateStoreAt(Path.Combine(_directory, "last-event-id.txt"));

    protected override ILastEventIdStore CreateStoreOverSameBackend() => CreateStore();

    protected override ILastEventIdStore CreateStoreWithFailingBackend() =>
        CreateStoreAt(Path.Combine(_directory, "missing", "last-event-id.txt"));

    private static FileLastEventIdStore CreateStoreAt(string path) => new(new FileLastEventIdStoreOptions { FilePath = path });
}
