namespace SsePulse.Client.Authentication.Providers.TokenProviders;

internal static class Constants
{
    public const string StaticTokenProviderName = "StaticTokenProvider";
    public const string ClientCredentialsTokenProviderName = "ClientCredentialsTokenProvider";
    public const string EnvironmentVariableTokenProviderName = "EnvironmentVariableTokenProvider";

    public const string BearerTokenSchemeName = "Bearer";
    public const string BasicSchemeName = "Basic";
}