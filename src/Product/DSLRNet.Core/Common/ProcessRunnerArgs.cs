namespace DSLRNet.Core.Common;

public class ProcessRunnerArgs<T>
{
    public T Context { get; set; }

    public string ExePath { get; set; }

    public string Arguments { get; set; }

    public int RetryCount = 0;

    public Microsoft.Extensions.Logging.ILogger? LoggerOverride { get; set; }
}
