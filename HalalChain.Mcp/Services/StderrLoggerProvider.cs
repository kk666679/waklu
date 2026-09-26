using Microsoft.Extensions.Logging;

namespace HalalChain.Mcp.Services;

/// <summary>
/// Minimal stderr logger.
///
/// The MCP stdio transport owns stdout, so anything written there by an
/// accidental <c>Console.WriteLine</c> corrupts the frame stream. Diagnostics
/// therefore go to stderr. This provider exists instead of
/// <c>Microsoft.Extensions.Logging.Console</c> so the server keeps its
/// dependency surface unchanged.
/// </summary>
internal sealed class StderrLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minimum;

    public StderrLoggerProvider(LogLevel minimum) => _minimum = minimum;

    public ILogger CreateLogger(string categoryName) => new StderrLogger(categoryName, _minimum);

    public void Dispose()
    {
        // Nothing unmanaged to release.
    }

    private sealed class StderrLogger : ILogger
    {
        private readonly string _category;
        private readonly LogLevel _minimum;

        public StderrLogger(string category, LogLevel minimum)
        {
            _category = category;
            _minimum = minimum;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimum;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var line = exception is null
                ? $"[{logLevel}] {_category}: {message}"
                : $"[{logLevel}] {_category}: {message} ({exception.GetType().Name}: {exception.Message})";

            // Console.Error is synchronised internally, so interleaving with the
            // stdout writer is safe.
            Console.Error.WriteLine(line);
        }
    }
}
