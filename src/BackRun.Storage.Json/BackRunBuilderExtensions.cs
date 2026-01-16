using BackRun.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BackRun.Storage.Json;

public static class BackRunBuilderExtensions
{
    public static IBackRunBuilder UseJsonFlatFileStorage(
        this IBackRunBuilder builder, 
        Action<JsonStorageOptions>? configure = null)
    {
        var options = new JsonStorageOptions();
        configure?.Invoke(options);

        var expandedPath = Environment.ExpandEnvironmentVariables(options.Path);
        
        builder.Services.AddScoped<IBackRunStorage>(_ => new JsonFlatFileStorage(expandedPath));

        return builder;
    }
}