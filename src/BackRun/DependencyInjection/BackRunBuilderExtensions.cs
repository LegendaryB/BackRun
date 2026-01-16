using System.Reflection;
using BackRun.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BackRun.DependencyInjection;

public static class BackRunBuilderExtensions
{
    extension(IBackRunBuilder builder)
    {
        /// <summary>
        /// Adds custom middleware to the job execution pipeline.
        /// </summary>
        public IBackRunBuilder AddMiddleware<TMiddleware>()
            where TMiddleware : class, IBackRunMiddleware
        {
            builder.Services.AddScoped<IBackRunMiddleware, TMiddleware>();
        
            return builder;
        }
        
        /// <summary>
        /// Adds custom middleware to the job execution pipeline if the specified condition is met.
        /// </summary>
        public IBackRunBuilder AddMiddlewareWhen<TMiddleware>(
            bool condition,
            Func<IBackRunBuilder, IBackRunBuilder> action)
        
            where TMiddleware : class, IBackRunMiddleware
        {
            return condition ? action(builder) : builder;
        }

        /// <summary>
        /// Registers a job handler with an internal key to ensure it can be resolved by the engine
        /// without leaking the concrete type to the public DI container.
        /// </summary>
        public IBackRunBuilder AddHandler<THandler>()
            where THandler : class, IBackRunJobHandler
        {
            builder.Services.AddKeyedTransient<THandler>(Constants.BackRunKeyedServiceKey);
        
            var interfaces = typeof(THandler)
                .GetInterfaces()
                .Where(@interface => 
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == typeof(IBackRunJobHandler<>));
        
            foreach (var @interface in interfaces)
            {
                builder.Services.AddTransient(
                    @interface,
                    sp => sp.GetRequiredKeyedService<THandler>(Constants.BackRunKeyedServiceKey));
            }
        
            return builder;    
        }

        /// <summary>
        /// Scans the specified assembly for all classes implementing IBackRunJobHandler and registers them.
        /// </summary>
        public IBackRunBuilder AddHandlersFromAssembly(Assembly assembly)
        {
            var handlerTypes = assembly
                .GetTypes()
                .Where(type =>
                    type is { IsClass: true, IsAbstract: false } &&
                    type.IsAssignableTo(typeof(IBackRunJobHandler)));

            return builder.AddHandlers(handlerTypes.ToArray());
        }
        
        /// <summary>
        /// Registers the types implementing IBackRunJobHandler as job handlers.
        /// </summary>
        public IBackRunBuilder AddHandlers(params Type[] handlerTypes)
        {
            var addHandlerMethod = typeof(BackRunBuilderExtensions).GetMethod(nameof(AddHandler))!;

            foreach (var handler in handlerTypes)
            {
                if (!handler.IsClass ||
                    handler.IsAbstract ||
                    !handler.IsAssignableTo(typeof(IBackRunJobHandler)))
                {
                    throw new ArgumentException(
                        $"Type '{handler.FullName}' is not a valid BackRun job handler. " +
                        $"Handlers must be non-abstract classes and implement IBackRunJobHandler.");
                }

                addHandlerMethod
                    .MakeGenericMethod(handler)
                    .Invoke(null, [builder]);
            }

            return builder;
        }
    }
}