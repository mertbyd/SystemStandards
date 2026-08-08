using Microsoft.Extensions.Logging;

namespace SystemStandards.Abp.Authorization.Tests.Fakes;

/// <summary>
/// Üretilen log satırlarını bellekte tutan sağlayıcı.
/// Ret gerekçesinin gerçekten loglandığını doğrulamak için kullanılır.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly Lock _sync = new();
    private readonly List<string> _messages = [];

    /// <summary>Kaydedilen log satırları.</summary>
    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_sync)
            {
                return _messages.ToList();
            }
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new RecordingLogger(this);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    // Bicimlenmis satiri kayda ekler.
    private void Record(string message)
    {
        lock (_sync)
        {
            _messages.Add(message);
        }
    }

    // Butun kategorileri ayni kayda yazan logger.
    private sealed class RecordingLogger : ILogger
    {
        private readonly RecordingLoggerProvider _provider;

        public RecordingLogger(RecordingLoggerProvider provider)
        {
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _provider.Record(formatter(state, exception));
        }
    }
}
