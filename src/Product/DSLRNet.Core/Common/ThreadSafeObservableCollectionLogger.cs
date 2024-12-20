using DSLRNet.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;

public class ThreadSafeObservableCollectionLogger : ILogger
{
    private readonly ThreadSafeObservableCollection<string> logMessages;

    public ThreadSafeObservableCollectionLogger(ThreadSafeObservableCollection<string> logMessages)
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

public class ThreadSafeObservableCollectionLoggerProvider : ILoggerProvider
{
    private readonly ThreadSafeObservableCollection<string> logMessages;

    public ThreadSafeObservableCollectionLoggerProvider(ThreadSafeObservableCollection<string> logMessages)
    {
        this.logMessages = logMessages;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ThreadSafeObservableCollectionLogger(logMessages);
    }

    public void Dispose() { }
}
