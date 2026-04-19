namespace SsePulse.Client.Authentication.Providers.Configurations;

/// <summary>
/// Configuration for <see cref="SsePulse.Client.Authentication.Providers.ApiKeyAuthenticationProvider"/>.
/// Specifies the API key value and the HTTP header to which it is applied.
/// </summary>
public class ApiKeyAuthenticationProviderConfiguration
{
    /// <summary>Gets or sets the API key value sent with each request.</summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the HTTP request header that carries the API key.
    /// Defaults to <c>X-API-Key</c>.
    /// </summary>
    public string Header { get; set; } = "X-API-Key";
}