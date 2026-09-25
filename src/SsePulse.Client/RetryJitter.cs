namespace SsePulse.Client;

/// <summary>Specifies how a random component is applied to the computed retry delay.</summary>
public enum RetryJitter
{
    /// <summary>The computed delay is used as is.</summary>
    None,

    /// <summary>
    /// The delay is a random value between zero and the computed delay. It spreads the attempts of many
    /// clients that lost the connection at the same time, so that they do not reconnect all together.
    /// </summary>
    Full
}
