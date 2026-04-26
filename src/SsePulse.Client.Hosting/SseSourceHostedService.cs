using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Hosting;

/// <summary>
/// Background service that starts consuming SSE events from a <see cref="SseSource"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/SsePulse.Client/docs/hosted-services.html"/>
/// </summary>
public class SseSourceHostedService : BackgroundService
{
    private readonly ISseSourceControl _sseSourceControl;
    private readonly ILogger<SseSourceHostedService> _logger;

    /// <summary>
    /// Creates a new <see cref="SseSourceHostedService"/> instance.
    /// </summary>
    /// <param name="sseSourceControl">The control used to start consuming events from the related SSE source.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/> when omitted.</param>
    public SseSourceHostedService(ISseSourceControl sseSourceControl, ILogger<SseSourceHostedService>? logger = null)
    {
        _sseSourceControl = sseSourceControl ?? throw new ArgumentNullException(nameof(sseSourceControl));
        _logger = logger ?? NullLogger<SseSourceHostedService>.Instance;
    }
    
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Start consuming from SseSource...");
            await _sseSourceControl.StartConsumeAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while consuming from SseSource");
            throw;
        }
        finally
        {
            _logger.LogInformation("Finished consuming from SseSource...");
        }
    }
}