namespace SsePulse.Client.Authentication.Providers;

public class ApiKeyAuthenticationProviderConfiguration
{
    public string Key { get; set; }

    public string Header { get; set; } = "X-API-Key";
}