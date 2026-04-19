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

/// <summary>
/// Extension methods on <see cref="ISseSourceBuilder"/> for configuring SSE authentication.
/// Supports bearer-token, Basic, API-key, and custom authentication providers.
/// </summary>
public static class SseBuilderExtensions
{
    /// <summary>The configuration section name that contains authentication settings (<c>Authentication</c>).</summary>
    private const string AuthenticationSectionName = "Authentication";

    /// <summary>The configuration key used to identify the authentication provider type (<c>Provider</c>).</summary>
    private const string AuthenticationProviderKeyName = "Provider";

    /// <summary>The configuration section name for provider-specific arguments (<c>Args</c>).</summary>
    private const string AuthenticationProviderArgumentsSectionName = "Args";

    /// <summary>The configuration key used to identify the token provider type within bearer-token config (<c>TokenProvider</c>).</summary>
    private const string TokenProviderKeyName = "TokenProvider";

    extension(ISseSourceBuilder builder)
    {
        /// <summary>
        /// Adds authentication to this SSE source.
        /// If the builder's configuration contains an <c>Authentication</c> section, the provider is
        /// resolved from configuration; otherwise, <see cref="AuthenticationRequestMutator"/> is registered
        /// without a pre-configured provider — register an <see cref="ISseAuthenticationProvider"/> in the
        /// DI container separately before the source is resolved.
        /// </summary>
        /// <returns>The same builder for chaining.</returns>
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

        /// <summary>
        /// Adds authentication using a pre-constructed <see cref="ISseAuthenticationProvider"/> instance.
        /// </summary>
        /// <param name="provider">The authentication provider to apply to every outgoing request.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddAuthentication(ISseAuthenticationProvider provider)
        {
            return builder.AddRequestMutator(_ => new AuthenticationRequestMutator(provider));
        }

        /// <summary>
        /// Adds authentication using an <see cref="ISseAuthenticationProvider"/> resolved from the DI container.
        /// Register <typeparamref name="TAuthenticationProvider"/> in the container before calling this method.
        /// </summary>
        /// <typeparam name="TAuthenticationProvider">The authentication provider type to resolve.</typeparam>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddAuthentication<TAuthenticationProvider>()
            where TAuthenticationProvider : ISseAuthenticationProvider
        {
            return builder.AddRequestMutator(sp =>
                new AuthenticationRequestMutator(sp.GetRequiredService<TAuthenticationProvider>()));
        }

        /// <summary>
        /// Adds authentication using a factory delegate that constructs the <see cref="ISseAuthenticationProvider"/>
        /// from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="authProviderFactory">Factory receiving the DI container and returning the provider.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddAuthentication(Func<IServiceProvider, ISseAuthenticationProvider> authProviderFactory)
        {
            return builder.AddRequestMutator(sp => new AuthenticationRequestMutator(authProviderFactory(sp)));
        }

        /// <summary>
        /// Adds authentication from a configuration section. The <c>Provider</c> key selects
        /// the authentication scheme (<c>Bearer</c>, <c>Basic</c>, or <c>ApiKey</c>).
        /// </summary>
        /// <param name="authConfiguration">The configuration section containing the provider type and its arguments.</param>
        /// <returns>The same builder for chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <c>Provider</c> value is not recognized.</exception>
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
        
        /// <summary>
        /// Adds Bearer-token authentication using an <see cref="SsePulse.Client.Authentication.Providers.BearerTokenAuthenticationProvider"/>
        /// registered in the DI container. Register the provider and its <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.Configurations.ITokenProvider"/> dependency separately.
        /// </summary>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddBearerTokenAuthentication()
        {
            builder.Services.AddSingleton<BearerTokenAuthenticationProvider>();
            return builder.AddAuthentication<BearerTokenAuthenticationProvider>();
        }
        
        /// <summary>
        /// Adds Bearer-token authentication using a pre-constructed <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.Configurations.ITokenProvider"/> instance.
        /// </summary>
        /// <param name="tokenProvider">The token provider supplying Bearer tokens.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddBearerTokenAuthentication(ITokenProvider tokenProvider)
        {
            return AddBearerTokenAuthentication(builder, _ => tokenProvider);
        }
        
        /// <summary>
        /// Adds Bearer-token authentication using a factory that resolves the <see cref="SsePulse.Client.Authentication.Providers.TokenProviders.Configurations.ITokenProvider"/> from the DI container.
        /// </summary>
        /// <param name="tokenProviderFactory">Factory receiving the <see cref="IServiceProvider"/> and returning a token provider.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddBearerTokenAuthentication(Func<IServiceProvider, ITokenProvider> tokenProviderFactory)
        {
            return builder.AddAuthentication(sp => new BearerTokenAuthenticationProvider(tokenProviderFactory.Invoke(sp)));
        }
        
        /// <summary>
        /// Adds Bearer-token authentication configured from a configuration section.
        /// The <c>TokenProvider</c> key within <c>Args</c> selects the token acquisition strategy
        /// (<c>Static</c>, <c>ClientCredentials</c>, or <c>EnvironmentVariable</c>).
        /// </summary>
        /// <param name="authConfiguration">The authentication configuration section.</param>
        /// <returns>The same builder for chaining.</returns>
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
        
        /// <summary>
        /// Adds Basic authentication using a <see cref="SsePulse.Client.Authentication.Providers.BasicAuthenticationProvider"/>
        /// registered in the DI container. Register the provider and its <see cref="SsePulse.Client.Authentication.Common.Credentials.BasicCredentials"/> dependency separately.
        /// </summary>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddBasicAuthentication()
        {
            builder.Services.AddSingleton<BasicAuthenticationProvider>();
            return builder.AddAuthentication<BasicAuthenticationProvider>();
        }
        
        /// <summary>
        /// Adds Basic authentication and configures the credentials through a delegate.
        /// The credentials are named and resolved from the DI options system.
        /// </summary>
        /// <param name="configureOptions">Delegate that populates <see cref="SsePulse.Client.Authentication.Common.Credentials.BasicCredentials"/>.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddBasicAuthentication(Action<BasicCredentials> configureOptions)
        {
            builder.Services.Configure(builder.Name, configureOptions);
            return builder.AddAuthentication(sp => new BasicAuthenticationProvider(
                sp.GetRequiredService<IOptionsMonitor<BasicCredentials>>().Get(builder.Name)));
        }
        
        /// <summary>
        /// Adds Basic authentication using a pre-built <see cref="SsePulse.Client.Authentication.Common.Credentials.BasicCredentials"/> instance.
        /// </summary>
        /// <param name="configuration">The username/password credentials to use.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddBasicAuthentication(BasicCredentials configuration)
        {
            return builder.AddAuthentication(_ => new BasicAuthenticationProvider(configuration));
        }

        /// <summary>
        /// Adds Basic authentication from a configuration section. Expects <see cref="SsePulse.Client.Authentication.Common.Credentials.BasicCredentials"/>
        /// to be bindable from the <c>Args</c> sub-section.
        /// </summary>
        /// <param name="authConfiguration">The authentication configuration section.</param>
        /// <returns>The same builder for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the <c>Args</c> section cannot be bound to <see cref="SsePulse.Client.Authentication.Common.Credentials.BasicCredentials"/>.</exception>
        public ISseSourceBuilder AddBasicAuthentication(IConfiguration authConfiguration)
        {
            ConfigurationSection argsSection = (ConfigurationSection)authConfiguration.GetSection(AuthenticationProviderArgumentsSectionName);
            BasicCredentials basicCredentials = 
                argsSection.Get<BasicCredentials>() ??
                throw new InvalidOperationException("Invalid configuration for Basic Authentication");
            return builder.AddAuthentication(_ => new BasicAuthenticationProvider(basicCredentials));
        }
        
        /// <summary>
        /// Adds API-key authentication using an <see cref="SsePulse.Client.Authentication.Providers.ApiKeyAuthenticationProvider"/>
        /// registered in the DI container. Register the provider and its configuration dependency separately.
        /// </summary>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddApiKeyAuthentication()
        {
            builder.Services.AddSingleton<ApiKeyAuthenticationProvider>();
            return builder.AddAuthentication<ApiKeyAuthenticationProvider>();
        }
        
        /// <summary>
        /// Adds API-key authentication and configures the key/header through a delegate.
        /// </summary>
        /// <param name="configureOptions">Delegate that populates <see cref="SsePulse.Client.Authentication.Providers.Configurations.ApiKeyAuthenticationProviderConfiguration"/>.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddApiKeyAuthentication(Action<ApiKeyAuthenticationProviderConfiguration> configureOptions)
        {
            builder.Services.Configure(builder.Name, configureOptions);
            return builder.AddAuthentication(sp => new ApiKeyAuthenticationProvider(
                sp.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationProviderConfiguration>>().Get(builder.Name)));
        }
        
        /// <summary>
        /// Adds API-key authentication using a pre-built <see cref="SsePulse.Client.Authentication.Providers.Configurations.ApiKeyAuthenticationProviderConfiguration"/> instance.
        /// </summary>
        /// <param name="configuration">The API key and target header name.</param>
        /// <returns>The same builder for chaining.</returns>
        public ISseSourceBuilder AddApiKeyAuthentication(ApiKeyAuthenticationProviderConfiguration configuration)
        {
            builder = builder.AddAuthentication(_ => new ApiKeyAuthenticationProvider(configuration));
            return builder;
        }

        /// <summary>
        /// Adds API-key authentication from a configuration section. Expects
        /// <see cref="SsePulse.Client.Authentication.Providers.Configurations.ApiKeyAuthenticationProviderConfiguration"/>
        /// to be bindable from the <c>Args</c> sub-section.
        /// </summary>
        /// <param name="authConfiguration">The authentication configuration section.</param>
        /// <returns>The same builder for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the <c>Args</c> section cannot be bound to <see cref="SsePulse.Client.Authentication.Providers.Configurations.ApiKeyAuthenticationProviderConfiguration"/>.</exception>
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