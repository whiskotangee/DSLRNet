namespace DSLRNet.Core.Common;

using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;
using System.Text;

public class ProcessRunner(ILogger<ProcessRunner> logger)
{
    private readonly SemaphoreSlim semaphoreSlim = new(100);
    private readonly ILogger logger = logger;

    public async Task<List<(T Context, string Output)>> RunProcessesAsync<T>(IEnumerable<ProcessRunnerArgs<T>> args)
    {
        try
        {
            List<Task<(T Context, string Output)>> tasks = [];
            foreach (ProcessRunnerArgs<T> process in args)
            {
                tasks.Add(this.RunProcessAsync(process));
            }

            await Task.WhenAll(tasks);

            return tasks.Select(t => t.Result).ToList();
        }
        catch (Exception ex)
        {
            this.logger.Log(LogLevel.Error, ex.ToString());
            throw;
        }
    }

    public async Task<(T Context, string Output)> RunProcessAsync<T>(ProcessRunnerArgs<T> args)
    {
        await this.semaphoreSlim.WaitAsync();

        try
        {
            if (args.RetryCount > 0)
            {
                return await Policy<(T Context, string Output)>.Handle<Exception>().RetryAsync(args.RetryCount).ExecuteAsync(() => this.RunProcess(args));
            }
            else
            {
                return await this.RunProcess(args);
            }
        }
        finally
        {
            this.semaphoreSlim.Release();
        }

    }

    private async Task<(T Context, string Output)> RunProcess<T>(ProcessRunnerArgs<T> args)
    {
        ILogger activeLogger = args.LoggerOverride ?? this.logger;

        activeLogger.Log(LogLevel.Information, $"Launching process {args.ExePath} {args.Arguments}");

        ProcessStartInfo processStartInfo = new()
        {
            FileName = args.ExePath,
            Arguments = args.Arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new()
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        StringBuilder outputBuilder = new();
        StringBuilder errorBuilder = new();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                activeLogger.Log(LogLevel.Information, e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                activeLogger.Log(LogLevel.Information, e.Data);
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"Process command |{processStartInfo.FileName} {processStartInfo.Arguments}| exited with code {process.ExitCode}. Error: {errorBuilder}");

        return (args.Context, string.Concat(outputBuilder.ToString(), errorBuilder.ToString()));
    }
}

