namespace SsePulse.Client.Authentication.Common.Credentials;

/// <summary>
/// Holds the username and password used for HTTP Basic authentication.
/// Pass an instance to <see cref="SsePulse.Client.Authentication.Providers.BasicAuthenticationProvider"/>
/// or supply it via configuration.
/// </summary>
public class BasicCredentials(string username, string password) : IAuthenticationCredentials
{
    /// <summary>Gets or sets the Basic auth username.</summary>
    public string Username { get; set; } = username;

    /// <summary>Gets or sets the Basic auth password.</summary>
    public string Password { get; set; } = password;

    /// <summary>Initializes a <see cref="BasicCredentials"/> with empty username and password (for configuration binding).</summary>
    public BasicCredentials() : this(string.Empty, string.Empty)
    {
    }
}