using BackRun.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BackRun.Storage.InMemory;

public static class BackRunBuilderExtensions
{
    public static IBackRunBuilder UseInMemoryStorage(this IBackRunBuilder builder)
    {   
        builder.Services.AddScoped<IBackRunStorage>(_ => new BackRunInMemoryStorage());

        return builder;
    }
}