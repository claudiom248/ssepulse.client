namespace SsePulse.Client.Authentication.Common.Credentials;

public class BasicCredentials(string username, string password) : IAuthenticationCredentials
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password;

    public BasicCredentials() : this(string.Empty, string.Empty)
    {
        
    }
}