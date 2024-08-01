namespace EventGridEmulator.Tests;

internal sealed class XunitLoggerProvider : ILoggerProvider, ILogger
{
    private static readonly Dictionary<LogLevel, string> LogLevelStrings = new Dictionary<LogLevel, string>
    {
        [LogLevel.None] = "NON",
        [LogLevel.Trace] = "TRC",
        [LogLevel.Debug] = "DBG",
        [LogLevel.Information] = "INF",
        [LogLevel.Warning] = "WRN",
        [LogLevel.Error] = "ERR",
        [LogLevel.Critical] = "CRT",
    };

    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        this._output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return this;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "We prefer local time as this is displayed to a user")]
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        this._output.WriteLine("[{0:HH:mm:ss:ffff} {1}] {2}", DateTime.Now, LogLevelStrings[logLevel], message);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return new NoopDisposable();
    }

    public void Dispose()
    {
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}