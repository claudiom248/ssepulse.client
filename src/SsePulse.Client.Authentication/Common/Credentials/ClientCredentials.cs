namespace SsePulse.Client.Authentication.Common.Credentials;

public class ClientCredentials(string clientId, string clientSecret) : IAuthenticationCredentials
{
    public string ClientId { get; } = clientId;
    public string ClientSecret { get; } = clientSecret;
}