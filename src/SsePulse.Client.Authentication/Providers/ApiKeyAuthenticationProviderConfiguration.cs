namespace SsePulse.Client.Authentication.Providers;

public class ApiKeyAuthenticationProviderConfiguration(string Key, string HeaderKey = "X-Api-Key")
{
    public string Key { get; } = Key;
    public string HeaderKey { get; } = HeaderKey;
}