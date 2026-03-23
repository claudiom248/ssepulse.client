namespace SsePulse.Client.Authentication.Common.Credentials;

public class BasicCredentials(string username, string password) : IAuthenticationCredentials
{
    public string Username { get; } = username;
    public string Password { get; } = password;
}