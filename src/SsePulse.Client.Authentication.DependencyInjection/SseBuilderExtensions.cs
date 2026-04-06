using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Common.Credentials;
using SsePulse.Client.Authentication.Internal;
using SsePulse.Client.Authentication.Providers;
using SsePulse.Client.Authentication.Providers.Configurations;
using SsePulse.Client.Authentication.Providers.TokenProviders;
using SsePulse.Client.Authentication.Providers.TokenProviders.Configurations;
using SsePulse.Client.DependencyInjection.Abstractions;

namespace SsePulse.Client.Authentication.DependencyInjection;

public static class SseBuilderExtensions
{
    public const string AuthenticationSectionName = "Authentication";
    public const string AuthenticationProviderKeyName = "Provider";
    public const string AuthenticationProviderArgumentsSectionName = "Args";
    public const string TokenProviderKeyName = "TokenProvider";
    
    extension(ISseSourceBuilder builder)
    {
        public ISseSourceBuilder AddAuthentication()
        {
            IConfigurationSection? configurationSection = builder.Configuration?.GetSection(AuthenticationSectionName);
            if (configurationSection?.Exists() ?? false)
            {
                builder = builder.AddAuthentication(configurationSection);
                return builder;
            }
            
            builder.Services.AddTransient<AuthenticationRequestMutator>();
            builder.AddRequestMutator<AuthenticationRequestMutator>();
            return builder;
        }

        public ISseSourceBuilder AddAuthentication(ISseAuthenticationProvider provider)
        {
            return builder.AddRequestMutator(_ => new AuthenticationRequestMutator(provider));
        }

        public ISseSourceBuilder AddAuthentication<TAuthenticationProvider>()
            where TAuthenticationProvider : ISseAuthenticationProvider
        {
            return builder.AddRequestMutator(sp =>
                new AuthenticationRequestMutator(sp.GetRequiredService<TAuthenticationProvider>()));
        }

        public ISseSourceBuilder AddAuthentication(Func<IServiceProvider, ISseAuthenticationProvider> authProviderFactory)
        {
            return builder.AddRequestMutator(sp => new AuthenticationRequestMutator(authProviderFactory(sp)));
        }

        public ISseSourceBuilder AddAuthentication(IConfiguration authConfiguration)
        {
            builder = authConfiguration.GetValue<string>(AuthenticationProviderKeyName) switch
            {
                Constants.BearerTokenAuthenticationProviderName => builder.AddBearerTokenAuthentication(authConfiguration),
                Constants.BasicCredentialsAuthenticationProviderName => builder.AddBasicAuthentication(authConfiguration),
                Constants.ApiKeyAuthenticationProviderName => builder.AddApiKeyAuthentication(authConfiguration),
                _ => throw new ArgumentOutOfRangeException()
            };
            return builder;
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication()
        {
            builder.Services.AddSingleton<BearerTokenAuthenticationProvider>();
            return builder.AddAuthentication<BearerTokenAuthenticationProvider>();
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication(ITokenProvider tokenProvider)
        {
            return AddBearerTokenAuthentication(builder, _ => tokenProvider);
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication(Func<IServiceProvider, ITokenProvider> tokenProviderFactory)
        {
            return builder.AddAuthentication(sp => new BearerTokenAuthenticationProvider(tokenProviderFactory.Invoke(sp)));
        }
        
        public ISseSourceBuilder AddBearerTokenAuthentication(IConfiguration authConfiguration)
        {
            ConfigurationSection argsSection = (ConfigurationSection)authConfiguration.GetSection(AuthenticationProviderArgumentsSectionName);
            ITokenProviderConfiguration config =
                argsSection.GetValue<string>(TokenProviderKeyName) switch
                {
                    Constants.ClientCredentialsTokenProviderName => authConfiguration
                        .Get<ClientCredentialsTokenProviderConfiguration>(),
                    Constants.StaticTokenProviderName => authConfiguration.Get<StaticTokenProviderConfiguration>(),
                    Constants.EnvironmentVariableTokenProviderName => authConfiguration
                        .Get<EnvironmentVariableTokenProviderConfiguration>(),
                    _ => throw new ArgumentException(nameof(ITokenProviderConfiguration.Provider))
                };
            return builder.AddAuthentication(_ =>
                new BearerTokenAuthenticationProvider(TokenProviderFactory.Create(config)));
        }
        
        public ISseSourceBuilder AddBasicAuthentication()
        {
            builder.Services.AddSingleton<BasicAuthenticationProvider>();
            return builder.AddAuthentication<BasicAuthenticationProvider>();
        }
        
        public ISseSourceBuilder AddBasicAuthentication(Action<BasicCredentials> configureOptions)
        {
            builder.Services.Configure(builder.Name, configureOptions);
            return builder.AddAuthentication(sp => new BasicAuthenticationProvider(
                sp.GetRequiredService<IOptionsMonitor<BasicCredentials>>().Get(builder.Name)));
        }
        
        public ISseSourceBuilder AddBasicAuthentication(BasicCredentials configuration)
        {
            return builder.AddAuthentication(_ => new BasicAuthenticationProvider(configuration));
        }

        public ISseSourceBuilder AddBasicAuthentication(IConfiguration authConfiguration)
        {
            ConfigurationSection argsSection = (ConfigurationSection)authConfiguration.GetSection(AuthenticationProviderArgumentsSectionName);
            BasicCredentials basicCredentials = 
                argsSection.Get<BasicCredentials>() ??
                throw new InvalidOperationException("Invalid configuration for Basic Authentication");
            return builder.AddAuthentication(_ => new BasicAuthenticationProvider(basicCredentials));
        }
        
        public ISseSourceBuilder AddApiKeyAuthentication()
        {
            builder.Services.AddSingleton<ApiKeyAuthenticationProvider>();
            return builder.AddAuthentication<ApiKeyAuthenticationProvider>();
        }
        
        public ISseSourceBuilder AddApiKeyAuthentication(Action<ApiKeyAuthenticationProviderConfiguration> configureOptions)
        {
            builder.Services.Configure(builder.Name, configureOptions);
            return builder.AddAuthentication(sp => new ApiKeyAuthenticationProvider(
                sp.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationProviderConfiguration>>().Get(builder.Name)));
        }
        
        public ISseSourceBuilder AddApiKeyAuthentication(ApiKeyAuthenticationProviderConfiguration configuration)
        {
            builder = builder.AddAuthentication(_ => new ApiKeyAuthenticationProvider(configuration));
            return builder;
        }

        public ISseSourceBuilder AddApiKeyAuthentication(IConfiguration authConfiguration)
        {
            ConfigurationSection argsSection = (ConfigurationSection)authConfiguration.GetSection(AuthenticationProviderArgumentsSectionName);
            ApiKeyAuthenticationProviderConfiguration configuration =
                argsSection.Get<ApiKeyAuthenticationProviderConfiguration>() ??
                throw new InvalidOperationException("Invalid configuration for API Key Authentication");
            builder = builder.AddAuthentication(_ => new ApiKeyAuthenticationProvider(configuration));
            return builder;
        }
    }
}