using MongoDB.Driver;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Extensions.Stores.Mongo.IntegrationTests;

[Trait("Category", "IntegrationTests")]
[Collection(MongoContainerCollection.Name)]
public sealed class MongoLastEventIdStoreContractTests : DurableLastEventIdStoreContract
{
    private readonly MongoContainerFixture _fixture;
    private readonly string _documentKey = Guid.NewGuid().ToString("N");

    public MongoLastEventIdStoreContractTests(MongoContainerFixture fixture)
    {
        _fixture = fixture;
    }

    protected override ILastEventIdStore CreateStore() => CreateStoreOver(_fixture.MongoClient);

    protected override ILastEventIdStore CreateStoreOverSameBackend() => CreateStoreOver(_fixture.MongoClient);

    protected override ILastEventIdStore CreateStoreWithFailingBackend() =>
        CreateStoreOver(new MongoClient("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=300"));

    private MongoLastEventIdStore CreateStoreOver(IMongoClient client) =>
        new(
            new MongoLastEventIdStoreOptions
            {
                DatabaseName = "sse_contract_tests",
                CollectionName = "last_event_ids",
                DocumentKey = _documentKey
            },
            client);
}
