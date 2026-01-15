using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using BackRun.Abstractions;

namespace BackRun
{
    internal class BackRunJobProcessor : IBackRunJobProcessor
    {
        private record HandlerMetadata(
            Type HandlerType,
            Type PayloadType,
            Func<object, object, CancellationToken, Task> ExecuteAsyncDelegate);

        private readonly ConcurrentDictionary<string, HandlerMetadata> _handlerMetadataCache = new();

        private readonly ILogger<BackRunJobProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;

        public BackRunJobProcessor(
            ILogger<BackRunJobProcessor> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default)
        {
            using var scope = _logger.BeginScope(
                "JobId = {JobId}, HandlerType = {HandlerType}",
                job.Id,
                job.HandlerType);

            _logger.LogInformation(
                "Processing job. (JobId: {JobId}, HandlerType = {HandlerType})",
                job.Id,
                job.HandlerType);

            await using var serviceScope = _serviceProvider.CreateAsyncScope();

            try
            {
                await ExecuteHandlerAsync(
                    serviceScope,
                    job,
                    cancellationToken);

                job.Status = BackRunJobStatus.Succeeded;
                job.CompletedAt = DateTime.UtcNow;
                job.Error = null;

                _logger.LogInformation(
                    "Job completed successfully. (JobId = {JobId}, HandlerType = {HandlerType})",
                    job.Id,
                    job.HandlerType);
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(
                    job,
                    ex,
                    cancellationToken);

                // Rethrow so the engine can handle retries
                throw;
            }

            await UpdateStatusAsync(
                serviceScope,
                job,
                cancellationToken);
        }

        private async Task ExecuteHandlerAsync(
            AsyncServiceScope scope,
            BackRunJob job,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                "Resolving handler metadata (JobId = {JobId}, HandlerType = {HandlerType}).",
                job.Id,
                job.HandlerType);

            var metadata = GetHandlerMetadata(job.HandlerType);

            _logger.LogDebug(
                "Resolved handler metadata (HandlerType = {HandlerType}, PayloadType = {PayloadType}).",
                metadata.HandlerType.FullName,
                metadata.PayloadType.FullName);

            var handler = ResolveHandler(scope.ServiceProvider, metadata.HandlerType);

            _logger.LogDebug(
                "Resolved handler (HandlerType = {HandlerType}, PayloadType = {PayloadType}).",
                metadata.HandlerType.FullName,
                metadata.PayloadType.FullName);

            var payload = JsonSerializer.Deserialize(
                job.PayloadJson,
                metadata.PayloadType)!;

            _logger.LogDebug(
                "Deserialized payload. (HandlerType = {HandlerType}, PayloadType = {PayloadType}).",
                metadata.HandlerType.FullName,
                metadata.PayloadType.FullName);

            await metadata.ExecuteAsyncDelegate(
                handler,
                payload,
                cancellationToken);

            _logger.LogDebug(
                "Executed handler (HandlerType = {HandlerType}, PayloadType = {PayloadType}).",
                metadata.HandlerType.FullName,
                metadata.PayloadType.FullName);
        }

        private Task HandleFailureAsync(
            BackRunJob job,
            Exception exception,
            CancellationToken cancellationToken)
        {
            job.Error = exception.ToString();

            job.Status = BackRunJobStatus.Queued;

            _logger.LogWarning(
                exception,
                "Job failed but is queued for retry (HandlerType = {HandlerType}).",
                job.HandlerType);
            
            return Task.CompletedTask;
        }

        private async Task UpdateStatusAsync(
            AsyncServiceScope scope,
            BackRunJob job,
            CancellationToken cancellationToken)
        {
            var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();

            await storage.UpdateJobStatusAsync(
                job.Id,
                job.Status,
                job.Error,
                cancellationToken);

            _logger.LogDebug(
                "Job status updated (Status = {Status}).",
                job.Status);
        }

        private HandlerMetadata GetHandlerMetadata(string handlerTypeName)
        {
            return _handlerMetadataCache.GetOrAdd(handlerTypeName, static typeName =>
            {
                var handlerType = Type.GetType(typeName, throwOnError: true)!;

                var interfaceType = handlerType
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBackRunJobHandler<>));

                var payloadType = interfaceType.GetGenericArguments()[0];

                var executeMethod = handlerType.GetMethod(nameof(IBackRunJobHandler<object>.ExecuteAsync))!;

                return new HandlerMetadata(
                    handlerType,
                    payloadType,
                    CreateDelegate(handlerType, payloadType, executeMethod));
            });
        }

        private object ResolveHandler(IServiceProvider serviceProvider, Type handlerType)
        {
            _logger.LogDebug(
                "Resolving handler (HandlerType = {HandlerType}).",
                handlerType.FullName);

            var handler = serviceProvider.GetService(handlerType);

            if (handler != null)
            {
                _logger.LogDebug(
                    "Resolved handler (HandlerType = {HandlerType}).",
                    handlerType.FullName);

                return handler;
            }

            handler = TryResolveByInterface(serviceProvider, handlerType);

            if (handler != null)
                return handler;

            _logger.LogError(
                "Failed to resolve handler (HandlerType = {HandlerType}).",
                handlerType.FullName);

            throw new InvalidOperationException(
                $"Cannot resolve handler for type {handlerType.FullName}");
        }

        private object? TryResolveByInterface(
            IServiceProvider serviceProvider,
            Type handlerType)
        {
            foreach (var @interface in GetCandidateHandlerInterfaces(handlerType))
            {
                var handler = serviceProvider.GetService(@interface);

                if (handler == null)
                    continue;

                _logger.LogDebug(
                    "Resolved handler (Interface = {Interface}).",
                    @interface.FullName);

                return handler;
            }

            return null;
        }

        private static IEnumerable<Type> GetCandidateHandlerInterfaces(Type handlerType)
        {
            return handlerType.GetInterfaces()
                .Where(@interface =>
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == typeof(IBackRunJobHandler<>) ||
                    @interface.GetInterfaces().Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IBackRunJobHandler<>))
                )
                .Distinct();
        }

        private static Type? FindJobHandlerInterface(Type type)
        {
            if (type == null)
                return null;

            var interfaces = type.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                if (@interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == typeof(IBackRunJobHandler<>))
                {
                    return @interface;
                }
            }

            if (type.BaseType != null &&
                type.BaseType != typeof(object))
            {
                return FindJobHandlerInterface(type.BaseType);
            }

            return null;
        }

        private static Func<object, object, CancellationToken, Task> CreateDelegate(
            Type handlerType,
            Type payloadType,
            MethodInfo method)
        {
            var handlerParam = Expression.Parameter(typeof(object), "handler");
            var payloadParam = Expression.Parameter(typeof(object), "payload");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var castHandler = Expression.Convert(handlerParam, handlerType);
            var castPayload = Expression.Convert(payloadParam, payloadType);

            var call = Expression.Call(
                castHandler,
                method,
                castPayload,
                ctParam);

            var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task>>(
                call, handlerParam, payloadParam, ctParam);

            return lambda.Compile();
        }
    }
}