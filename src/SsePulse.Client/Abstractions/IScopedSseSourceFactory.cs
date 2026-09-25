namespace SsePulse.Client.Abstractions;

/// <summary>
/// Scoped version of <see cref="ISseSourceFactory"/>. Use this interface when you need to create SSE sources
/// programmatically at runtime within a scoped service or a hosted service.
/// </summary>
public interface IScopedSseSourceFactory : ISseSourceFactory
{
    
}