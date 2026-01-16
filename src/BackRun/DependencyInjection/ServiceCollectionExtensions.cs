using BackRun.Abstractions;
using BackRun.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace BackRun.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IBackRunBuilder AddBackRun(
            this IServiceCollection services,
            Action<BackRunOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            
            services.Configure(configure);
            
            services.AddSingleton<IBackRunJobEngine, BackRunJobEngine>();
            services.AddHostedService(sp => (BackRunJobEngine)sp.GetRequiredService<IBackRunJobEngine>());
            services.AddScoped<IBackRunJobProcessor, BackRunJobProcessor>();

            services.AddSingleton<IBackRunMiddleware, BackRunLoggingMiddleware>();
            services.AddScoped<IBackRunMiddleware, BackRunStatusMiddleware>();
            
            return new BackRunBuilder(services);
        }
    }
}
