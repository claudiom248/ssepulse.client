using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection.Extensions;

public static class SseSourceBuilderExtensions
{
    extension(ISseSourceBuilder builder)
    {
        public ISseSourceBuilder AddLastEventId()
        {
            builder.Services.TryAddTransient<InMemoryLastEventIdStore>();
            builder.Services.TryAddTransient<ILastEventIdStore>(sp => sp.GetRequiredService<InMemoryLastEventIdStore>());
            builder.Services.Configure<SseSourceFactoryOptions>(builder.Name, options =>
            {
                options.LastEventIdStoreFactory = sp => sp.GetRequiredService<InMemoryLastEventIdStore>();
            });
            builder.Services.TryAddTransient<LastEventIdRequestMutator>();
            builder.AddRequestMutator<LastEventIdRequestMutator>();
            return builder;
        }
        
        public ISseSourceBuilder AddLastEventId<TEventIdStore>()
            where TEventIdStore : class, ILastEventIdStore
        {
            builder.Services.Configure<SseSourceFactoryOptions>(builder.Name, options =>
            {
                options.LastEventIdStoreFactory = sp => sp.GetRequiredService<TEventIdStore>();
            });
            builder.AddRequestMutator(sp => new LastEventIdRequestMutator(sp.GetRequiredService<TEventIdStore>()));
            return builder;
        }
    }
}