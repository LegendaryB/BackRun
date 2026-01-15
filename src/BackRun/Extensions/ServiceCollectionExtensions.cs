using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackRun.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBackRun(this IServiceCollection services)
        {
            services.AddSingleton<BackRunJobEngine>();
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<BackRunJobEngine>());
            services.AddSingleton<IBackRunJobEngine>(sp => sp.GetRequiredService<BackRunJobEngine>());
            services.AddSingleton<IBackRunJobProcessor, BackRunJobProcessor>();

            return services;
        }
    }
}
