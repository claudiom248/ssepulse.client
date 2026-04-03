namespace SsePulse.Client.Authentication.Common.Credentials;

public class ClientCredentials(string clientId, string clientSecret) : IAuthenticationCredentials
{
    public string ClientId { get; set; } = clientId;
    public string ClientSecret { get; set; } = clientSecret;
    
    public ClientCredentials() : this(string.Empty, string.Empty)
    {
        
    }
}