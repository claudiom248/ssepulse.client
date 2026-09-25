using Microsoft.Extensions.Caching.Distributed;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Extensions.Stores.DistributedCache.IntegrationTests;

[Trait("Category", "IntegrationTests")]
[Collection(RedisContainerCollection.Name)]
public sealed class RedisLastEventIdStoreContractTests : DurableLastEventIdStoreContract
{
    private readonly RedisContainerFixture _fixture;
    private readonly string _key = Guid.NewGuid().ToString("N");

    public RedisLastEventIdStoreContractTests(RedisContainerFixture fixture)
    {
        _fixture = fixture;
    }

    protected override ILastEventIdStore CreateStore() => CreateStoreOver(_fixture.Cache);

    protected override ILastEventIdStore CreateStoreOverSameBackend() => CreateStoreOver(_fixture.Cache);

    protected override ILastEventIdStore CreateStoreWithFailingBackend() => CreateStoreOver(new ThrowingDistributedCache());

    private DistributedCacheLastEventIdStore CreateStoreOver(IDistributedCache cache) =>
        new(new DistributedCacheLastEventIdStoreOptions { Key = _key }, cache);
}
