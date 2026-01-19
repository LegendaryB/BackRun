using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using BackRun.Abstractions;

namespace BackRun
{
    internal sealed class BackRunJobProcessor(
        IServiceProvider serviceProvider,
        IEnumerable<IBackRunMiddleware> middlewares) : IBackRunJobProcessor
    {
        private static readonly ConcurrentDictionary<string, Func<object, object, CancellationToken, Task>>
            DelegateCache = new();

        public async Task ProcessJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default)
        {
            Func<BackRunJob, Task> pipeline = j => ExecuteHandlerInternalAsync(
                j,
                cancellationToken);

            foreach (var middleware in middlewares.Reverse())
            {
                var next = pipeline;
                
                pipeline = (j) => middleware.ExecuteAsync(
                    j,
                    next,
                    cancellationToken);
            }

            await pipeline(job);
        }

        private async Task ExecuteHandlerInternalAsync(
            BackRunJob job,
            CancellationToken cancellationToken)
        {
            var handlerType = Type.GetType(job.HandlerType)
                ?? throw new InvalidOperationException($"Could not find handler type: {job.HandlerType}");

            var payloadType = Type.GetType(job.PayloadType)
                ?? throw new InvalidOperationException($"Could not find payload type: {job.PayloadType}");

            var handler = serviceProvider.GetRequiredKeyedService(handlerType, Constants.BackRunKeyedServiceKey);
            
            var payload = JsonSerializer.Deserialize(job.PayloadJson, payloadType)
                ?? throw new InvalidOperationException("Payload deserialization returned null.");

            var runDelegate =
                DelegateCache.GetOrAdd(job.HandlerType, _ => CompileHandlerDelegate(handlerType, payloadType));

            await runDelegate(handler, payload, cancellationToken);
        }

        private static Func<object, object, CancellationToken, Task> CompileHandlerDelegate(Type handlerType,
            Type payloadType)
        {
            var handlerParam = Expression.Parameter(
                typeof(object),
                "handler");
            
            var payloadParam = Expression.Parameter(
                typeof(object),
                "payload");
            
            var cancellationTokenParam = Expression.Parameter(
                typeof(CancellationToken),
                "cancellationToken");

            var castHandler = Expression.Convert(
                handlerParam,
                handlerType);
            
            var castPayload = Expression.Convert(
                payloadParam,
                payloadType);

            var method = handlerType.GetMethod(nameof(IBackRunJobHandler<>.ExecuteAsync), [payloadType, typeof(CancellationToken)])
                 ?? throw new InvalidOperationException(
                     $"Method ExecuteAsync not found on {handlerType.FullName}");

            var call = Expression.Call(
                castHandler,
                method,
                castPayload,
                cancellationTokenParam);

            var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task>>(
                call,
                handlerParam,
                payloadParam,
                cancellationTokenParam);
            
            return lambda.Compile();
        }
    }
}