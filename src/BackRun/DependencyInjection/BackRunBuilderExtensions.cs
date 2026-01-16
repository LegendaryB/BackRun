using System.Reflection;
using BackRun.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BackRun.DependencyInjection;

public static class BackRunBuilderExtensions
{
    extension(IBackRunBuilder builder)
    {
        public IBackRunBuilder AddMiddleware<TMiddleware>()
            where TMiddleware : class, IBackRunMiddleware
        {
            builder.Services.AddScoped<IBackRunMiddleware, TMiddleware>();
        
            return builder;
        }

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

        public IBackRunBuilder AddHandlersFromAssembly(Assembly assembly)
        {
            var handlers = assembly
                .GetTypes()
                .Where(type =>
                    type is { IsClass: true, IsAbstract: false } &&
                    type.IsAssignableTo(typeof(IBackRunJobHandler)));

            foreach (var handler in handlers)
            {
                typeof(BackRunBuilderExtensions)
                    .GetMethod(nameof(AddHandler))!
                    .MakeGenericMethod(handler)
                    .Invoke(null, [builder]);
            }
            
            return builder;
        }
    }
}