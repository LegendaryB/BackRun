using BackRun.Abstractions;
using BackRun.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace BackRun.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBackRun(
            this IServiceCollection services,
            Action<BackRunOptions, IBackRunBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            
            var options = new BackRunOptions();
            var builder = new BackRunBuilder(services);

            builder.AddMiddleware<BackRunLoggingMiddleware>();
            builder.AddMiddleware<BackRunStatusMiddleware>();
            
            configure(options, builder);
            
            services.AddSingleton(options);
            
            services.AddSingleton<IBackRunJobEngine, BackRunJobEngine>();
            services.AddHostedService(sp => (BackRunJobEngine)sp.GetRequiredService<IBackRunJobEngine>());
            services.AddScoped<IBackRunJobProcessor, BackRunJobProcessor>();

            return services;
        }
    }
}
