using SsePulse.Client.Core;

namespace SsePulse.Client.Abstractions;

/// <summary>
/// Factory for creating named <see cref="SseSource"/> instances from the dependency-injection container.
/// Resolve this interface when you need to create SSE sources programmatically at runtime.
/// </summary>
public interface ISseSourceFactory
{
    /// <summary>
    /// Creates a configured <see cref="SseSource"/> for the registered source with the given <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The name used when the source was registered via <c>AddSseSource</c>.
    /// Pass <see langword="null"/> to use the default source name.
    /// </param>
    /// <returns>A ready-to-use <see cref="SseSource"/> instance.</returns>
    SseSource CreateSseSource(string? name);
}