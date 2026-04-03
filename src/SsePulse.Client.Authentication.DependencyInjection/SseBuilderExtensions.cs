using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Internal;
using SsePulse.Client.Authentication.Providers;
using SsePulse.Client.Authentication.Providers.TokenProviders;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;
using SsePulse.Client.DependencyInjection.Abstractions;
using Constants = SsePulse.Client.Authentication.Providers.TokenProviders.Constants;

namespace SsePulse.Client.Authentication.DependencyInjection;

public static class SseBuilderExtensions
{
    extension(ISseSourceBuilder builder)
    {
        public ISseSourceBuilder AddAuthentication()
        {
            builder.Services.AddTransient<AuthenticationRequestMutator>();
            builder = builder.AddRequestMutator<AuthenticationRequestMutator>();
            return builder;
        }

        public ISseSourceBuilder AddAuthentication(ISseAuthenticationProvider provider)
        {
            builder = builder.AddRequestMutator(_ => new AuthenticationRequestMutator(provider));
            return builder;
        }

        public ISseSourceBuilder AddAuthentication<TAuthenticationProvider>()
            where TAuthenticationProvider : ISseAuthenticationProvider
        {
            builder = builder.AddRequestMutator(sp =>
                new AuthenticationRequestMutator(sp.GetRequiredService<TAuthenticationProvider>()));
            return builder;
        }

        public ISseSourceBuilder AddAuthentication(Func<IServiceProvider, ISseAuthenticationProvider> authProviderFactory)
        {
            builder = builder.AddRequestMutator(sp => new AuthenticationRequestMutator(authProviderFactory(sp)));
            return builder;
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication()
        {
            builder.Services.AddSingleton<BearerTokenAuthenticationProvider>();
            builder = builder.AddAuthentication<BearerTokenAuthenticationProvider>();
            return builder;
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication(ITokenProvider tokenProvider)
        {
            return AddBearerTokenAuthentication(builder, _ => tokenProvider);
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication(Func<IServiceProvider, ITokenProvider> tokenProviderFactory)
        {
            builder = builder.AddAuthentication(sp => new BearerTokenAuthenticationProvider(tokenProviderFactory.Invoke(sp)));
            return builder;
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication(IConfiguration authConfiguration)
        {
            ITokenProviderConfiguration config =
                authConfiguration.GetValue<string>(nameof(ITokenProviderConfiguration.ProviderName)) switch
                {
                    Constants.ClientCredentialsTokenProviderName => authConfiguration
                        .Get<ClientCredentialsTokenProviderConfiguration>(),
                    Constants.StaticTokenProviderName => authConfiguration.Get<StaticTokenProviderConfiguration>(),
                    Constants.EnvironmentVariableTokenProviderName => authConfiguration
                        .Get<EnvironmentVariableTokenProviderConfiguration>(),
                    _ => throw new ArgumentException(nameof(ITokenProviderConfiguration.ProviderName))
                };
            builder = builder.AddAuthentication(_ =>
                new BearerTokenAuthenticationProvider(TokenProviderFactory.Create(config)));
            return builder;
        }

        // public ISseSourceBuilder AddBearerTokenAuthentication<TConfiguration>(
        //     IConfiguration authConfiguration,
        //     Func<TConfiguration, ITokenProvider> tokenProviderFactory)
        //     where TConfiguration : ITokenProviderConfiguration
        // {
        //     TConfiguration configuration = authConfiguration.Get<TConfiguration>()!;
        //     builder.Services.AddSingleton<ITokenProvider>(sp => tokenProviderFactory(configuration));
        //     builder.AddAuthentication<BearerTokenAuthenticationProvider>();
        //     return builder;
        // }
        
        public ISseSourceBuilder AddBasicAuthentication()
        {
            builder.Services.AddSingleton<BasicAuthenticationProvider>();
            builder = builder.AddAuthentication<BasicAuthenticationProvider>();
            return builder;
        }
        
        public ISseSourceBuilder AddBasicAuthentication(Action<BasicCredentials> configureOptions)
        {
            builder.Services.Configure(builder.Name, configureOptions);
            builder = builder.AddAuthentication(sp => new BasicAuthenticationProvider(
                sp.GetRequiredService<IOptionsMonitor<BasicCredentials>>().Get(builder.Name)));
            return builder;
        }
        
        public ISseSourceBuilder AddBasicAuthentication(BasicCredentials configuration)
        {
            builder = builder.AddAuthentication(_ => new BasicAuthenticationProvider(configuration));
            return builder;
        }

        public ISseSourceBuilder AddBasicAuthentication(IConfiguration authConfiguration)
        {
            BasicCredentials basicCredentials = 
                authConfiguration.Get<BasicCredentials>() ??
                throw new InvalidOperationException("Invalid configuration for Basic Authentication");
            builder = builder.AddAuthentication(_ => new BasicAuthenticationProvider(basicCredentials));
            return builder;
        }
        
        public ISseSourceBuilder AddApiKeyAuthentication()
        {
            builder.Services.AddSingleton<ApiKeyAuthenticationProvider>();
            builder = builder.AddAuthentication<ApiKeyAuthenticationProvider>();
            return builder;
        }
        
        public ISseSourceBuilder AddApiKeyAuthentication(Action<ApiKeyAuthenticationProviderConfiguration> configureOptions)
        {
            builder.Services.Configure(builder.Name, configureOptions);
            builder = builder.AddAuthentication(sp => new ApiKeyAuthenticationProvider(
                sp.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationProviderConfiguration>>().Get(builder.Name)));
            return builder;
        }
        
        public ISseSourceBuilder AddApiKeyAuthentication(ApiKeyAuthenticationProviderConfiguration configuration)
        {
            builder = builder.AddAuthentication(_ => new ApiKeyAuthenticationProvider(configuration));
            return builder;
        }

        public ISseSourceBuilder AddApiKeyAuthentication(IConfiguration authConfiguration)
        {
            ApiKeyAuthenticationProviderConfiguration configuration =
                authConfiguration.Get<ApiKeyAuthenticationProviderConfiguration>() ??
                throw new InvalidOperationException("Invalid configuration for API Key Authentication");
            builder = builder.AddAuthentication(_ => new ApiKeyAuthenticationProvider(configuration));
            return builder;
        }
    }
}