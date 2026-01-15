using Microsoft.Extensions.Logging;

namespace BackRun.Logging;

internal static partial class Engine
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stored job (JobId = {JobId}).")]
    internal static partial void LogJobStored(
        this ILogger<BackRunJobEngine> logger,
        Guid jobId);
    
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Handler type {HandlerType} does not have a valid AssemblyQualifiedName.")]
    internal static partial void LogHandlerHasNoValidAssemblyQualifiedName(
        this ILogger<BackRunJobEngine> logger,
        string? handlerType);
}