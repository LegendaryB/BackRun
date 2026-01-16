using BackRun.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BackRun.DependencyInjection;

internal sealed class BackRunBuilder(IServiceCollection services) : IBackRunBuilder
{
    public IServiceCollection Services { get; } = services;
}