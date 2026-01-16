using Microsoft.Extensions.DependencyInjection;

namespace BackRun.Abstractions;

public interface IBackRunBuilder
{
    IServiceCollection Services { get; }
}