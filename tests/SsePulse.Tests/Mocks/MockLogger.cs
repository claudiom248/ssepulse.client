using Microsoft.Extensions.Logging;

namespace SsePulse.Tests.Mocks;

/// <summary>
/// Mock logger that captures log entries for testing purposes
/// </summary>
public class MockLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logs = new();

    public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        _logs.Add(new LogEntry(logLevel, message, exception));
    }

    public bool HasLog(LogLevel level, string messageContains)
    {
        return _logs.Any(log => log.LogLevel == level && log.Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasLog(LogLevel level, string messageContains, Type exceptionType)
    {
        return _logs.Any(log =>
            log.LogLevel == level &&
            log.Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase) &&
            log.Exception?.GetType() == exceptionType);
    }

    public int CountLogs(LogLevel level)
    {
        return _logs.Count(log => log.LogLevel == level);
    }

    public void Clear()
    {
        _logs.Clear();
    }

    public record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);
}
