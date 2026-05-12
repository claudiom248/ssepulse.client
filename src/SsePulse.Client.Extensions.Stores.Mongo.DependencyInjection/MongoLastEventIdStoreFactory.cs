using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection;

internal class MongoLastEventIdStoreFactory : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<MongoLastEventIdStoreOptions> _optionsMonitor;
    private readonly Dictionary<string, IMongoClient> _ownedClients = [];
    
    public MongoLastEventIdStoreFactory(IOptionsMonitor<MongoLastEventIdStoreOptions> optionsMonitor, IServiceProvider serviceProvider)
    {
        _optionsMonitor = optionsMonitor;
        _serviceProvider = serviceProvider;
    }

    public MongoLastEventIdStore Create(string sourceName, Func<IServiceProvider, IMongoClient>? mongoClientFactory)
    {
        MongoLastEventIdStoreOptions options = _optionsMonitor.Get(sourceName);
        IMongoClient client = mongoClientFactory is not null 
            ? mongoClientFactory(_serviceProvider) 
            : _serviceProvider.GetRequiredService<IMongoClient>();
        return ActivatorUtilities.CreateInstance<MongoLastEventIdStore>(_serviceProvider, options, client);
    }
    
    public MongoLastEventIdStore Create(string sourceName, string connectionString)
    {
        MongoLastEventIdStoreOptions options = _optionsMonitor.Get(sourceName);
        IMongoClient client;
        if (_ownedClients.TryGetValue(connectionString, out client!))
        {
            return ActivatorUtilities.CreateInstance<MongoLastEventIdStore>(_serviceProvider, client);
        }
        client = new MongoClient(connectionString);
        _ownedClients.Add(connectionString, client);
        return ActivatorUtilities.CreateInstance<MongoLastEventIdStore>(_serviceProvider, options, client);
    }

    public void Dispose()
    {
        foreach(IMongoClient client in _ownedClients.Values)
        {
#if NET8_0_OR_GREATER
            client.Dispose();
// #else
//             
//             (client as MongoClient)?.Dispose();
#endif
        }
    }
}