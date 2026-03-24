namespace SsePulse.Client.Core.Abstractions;

internal interface IRequestMutator
{
    ValueTask ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken);
}