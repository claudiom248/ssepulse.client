namespace SsePulse.Client.Core.Abstractions;

/// <summary>
/// Interface for mutating outgoing HTTP requests made by the client. 
/// </summary>
public interface IRequestMutator
{
    /// <summary>
    /// Mutates the outgoing HTTP request. This can include adding headers, query parameters, or modifying the request in other ways.
    /// </summary>
    /// <param name="message">The HTTP request message to mutate.</param>
    /// <param name="cancellationToken">Token to cancel the mutation.</param>
    /// <returns></returns>
    ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken);
}