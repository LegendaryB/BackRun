using BackRun.Middleware;
using Microsoft.Extensions.Logging;

namespace BackRun.Logging;

internal static partial class BackRunLoggingMiddlewareLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting job execution (Handler: {HandlerType}).")]
    internal static partial void LogJobExecutionStarting(
        this ILogger<BackRunLoggingMiddleware> logger,
        string handlerType);
    
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Job completed successfully in {ElapsedMilliseconds}ms")]
    internal static partial void LogJobCompletedSuccessfully(
        this ILogger<BackRunLoggingMiddleware> logger,
        long elapsedMilliseconds);
    
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Job was canceled after {ElapsedMilliseconds}ms")]
    internal static partial void LogJobCancelledAfter(
        this ILogger<BackRunLoggingMiddleware> logger,
        long elapsedMilliseconds);
    
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Job failed after {ElapsedMilliseconds}ms with error: {ErrorMessage}")]
    internal static partial void LogJobFailedAfter(
        this ILogger<BackRunLoggingMiddleware> logger,
        Exception ex,
        long elapsedMilliseconds,
        string errorMessage);
}