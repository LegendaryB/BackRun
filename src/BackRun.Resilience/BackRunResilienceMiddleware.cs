using BackRun.Abstractions;
using Polly;

namespace BackRun.Resilience;

internal sealed class BackRunResilienceMiddleware(ResiliencePipeline pipeline) : IBackRunMiddleware
{
    public async Task ExecuteAsync(
        BackRunJob job,
        Func<BackRunJob, Task> next,
        CancellationToken cancellationToken = default)
    {
        var context = ResilienceContextPool.Shared.Get(cancellationToken);
        context.Properties.Set(new ResiliencePropertyKey<BackRunJob>("BackRunJob"), job);

        try
        {
            await pipeline.ExecuteAsync(async _ =>
            {
                await next(job);
            }, context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }
}