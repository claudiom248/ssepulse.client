using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Stores;

public sealed class InMemoryLastEventIdStoreContractTests : LastEventIdStoreContract
{
    protected override ILastEventIdStore CreateStore() => new InMemoryLastEventIdStore();
}
