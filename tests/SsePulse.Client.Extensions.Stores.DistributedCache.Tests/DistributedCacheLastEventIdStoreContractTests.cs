using Microsoft.Extensions.Caching.Distributed;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Extensions.Stores.DistributedCache.Tests;

public sealed class DistributedCacheLastEventIdStoreContractTests : DurableLastEventIdStoreContract
{
    private readonly InMemoryDistributedCache _cache = new();
    private readonly string _key = Guid.NewGuid().ToString("N");

    protected override ILastEventIdStore CreateStore() => CreateStoreOver(_cache);

    protected override ILastEventIdStore CreateStoreOverSameBackend() => CreateStoreOver(_cache);

    protected override ILastEventIdStore CreateStoreWithFailingBackend() => CreateStoreOver(new ThrowingDistributedCache());

    private DistributedCacheLastEventIdStore CreateStoreOver(IDistributedCache cache) =>
        new(new DistributedCacheLastEventIdStoreOptions { Key = _key }, cache);
}
