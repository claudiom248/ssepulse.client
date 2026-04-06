namespace SsePulse.Client.Authentication.Providers;

internal static class Constants
{
    public const string StaticTokenProviderName = "Static";
    public const string ClientCredentialsTokenProviderName = "ClientCredentials";
    public const string EnvironmentVariableTokenProviderName = "EnvironmentVariable";

    public const string BearerTokenAuthenticationProviderName = "BearerToken";
    public const string BasicCredentialsAuthenticationProviderName = "Basic";
    public const string ApiKeyAuthenticationProviderName = "ApiKey";
    
    public const string BearerTokenSchemeName = "Bearer";
    public const string BasicSchemeName = "Basic";
}