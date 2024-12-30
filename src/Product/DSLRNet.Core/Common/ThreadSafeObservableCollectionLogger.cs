namespace DSLRNet.Core.Common;

public class CollectionLogger(ICollection<string> logMessages) : ILogger
{
    private readonly ICollection<string> logMessages = logMessages;

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter != null)
        {
            string message = formatter(state, exception);
            logMessages.Add($"{logLevel}: {message}");
        }
    }
}

public class CollectionLoggerProvider(ICollection<string> logMessages) : ILoggerProvider
{
    private readonly ICollection<string> logMessages = logMessages;
    private bool disposedValue;

    public ILogger CreateLogger(string categoryName)
    {
        return new CollectionLogger(logMessages);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
