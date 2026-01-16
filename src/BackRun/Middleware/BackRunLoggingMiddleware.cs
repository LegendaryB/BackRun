using System.Diagnostics;
using BackRun.Abstractions;
using Microsoft.Extensions.Logging;

using static BackRun.Logging.BackRunLoggingMiddlewareLogMessages;

namespace BackRun.Middleware;

internal class BackRunLoggingMiddleware(ILogger<BackRunLoggingMiddleware> logger) : IBackRunMiddleware
{
    public async Task ExecuteAsync(
        BackRunJob job,
        Func<BackRunJob, Task> next,
        CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["JobId"] = job.Id });

        logger.LogJobExecutionStarting(job.HandlerType);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await next(job);
            stopwatch.Stop();

            logger.LogJobCompletedSuccessfully(stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            logger.LogJobCancelledAfter(stopwatch.ElapsedMilliseconds);
            
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            logger.LogJobFailedAfter(
                ex,
                stopwatch.ElapsedMilliseconds, 
                ex.Message);
            
            throw;
        }
    }
}