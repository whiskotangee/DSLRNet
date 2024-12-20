namespace DSLRNet.Common;

public class CollectionLogger : ILogger
{
    private readonly ICollection<string> logMessages;

    public CollectionLogger(ICollection<string> logMessages)
    {
        this.logMessages = logMessages;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter != null)
        {
            string message = formatter(state, exception);
            logMessages.Add($"{logLevel}: {message}");
        }
    }
}

public class CollectionLoggerProvider : ILoggerProvider
{
    private readonly ICollection<string> logMessages;

    public CollectionLoggerProvider(ICollection<string> logMessages)
    {
        this.logMessages = logMessages;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CollectionLogger(logMessages);
    }

    public void Dispose() { }
}
