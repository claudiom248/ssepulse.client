using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;

namespace SsePulse.Client.Authentication.Internal.Extensions.Configurations;

public static class SseSourceOptionsExtensions
{
    extension(SseSourceOptions options)
    {
        public SseSourceOptions AddAuthentication(ISseAuthenticationProvider provider)
        {
            List<IRequestMutator> requestMutators = options.RequestMutators.ToList();
            requestMutators.Add(new AuthenticationRequestMutator(provider));
            options.RequestMutators = requestMutators;
            return options;
        }
    }
}