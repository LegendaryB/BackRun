using BackRun.Abstractions;
using Microsoft.Extensions.Logging;

namespace BackRun.Logging;

internal static partial class BackRunJobEngineLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "BackRun Engine started (MaxDegreeOfParallelism: {MaxDegreeOfParallelism}).")]
    internal static partial void LogJobEngineStarted(
        this ILogger<BackRunJobEngine> logger,
        int maxDegreeOfParallelism);
    
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Exception in BackRun Engine main loop.")]
    internal static partial void LogJobEngineMainLoopException(
        this ILogger<BackRunJobEngine> logger,
        Exception ex);
    
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "BackRun Engine stopping. Waiting for {Count} tasks...")]
    internal static partial void LogJobEngineStopped(
        this ILogger<BackRunJobEngine> logger,
        int count);
    
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Job {JobId} finished with exception.")]
    internal static partial void LogJobFinishedWithException(
        this ILogger<BackRunJobEngine> logger,
        Exception ex,
        Guid jobId);
    
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Recovering orphaned job {JobId} (Status: {Status})")]
    internal static partial void LogRecoveringOrphanedJob(
        this ILogger<BackRunJobEngine> logger,
        Guid jobId,
        BackRunJobStatus status);
    
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to recover orphaned jobs during startup.")]
    internal static partial void LogRecoveringOrphanedJobFailed(
        this ILogger<BackRunJobEngine> logger,
        Exception ex);
    
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Exception in Scheduler Poller.")]
    internal static partial void LogExceptionInSchedulerPoller(
        this ILogger<BackRunJobEngine> logger,
        Exception ex);
}