using BackRun.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace BackRun.Resilience;

public static class BackRunBuilderExtensions
{
    extension(IBackRunBuilder builder)
    {
        /// <summary>
        /// Adds a resilience pipeline with a default strategy: 15m Timeout and 3 Exponential Retries with Jitter.
        /// </summary>
        public IBackRunBuilder AddResilience()
        {
            return builder.AddResilience(pipeline =>
            {
                pipeline.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMinutes(15) });

                pipeline.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    OnRetry = args =>
                    {
                        if (args.Outcome.Exception is null ||
                            !args.Context.Properties.TryGetValue(
                                new ResiliencePropertyKey<BackRunJob>("BackRunJob"),
                                out var job))
                        {
                            return default;
                        }
                        
                        job.RetryCount++;
                        job.LastError = $"Retry {args.AttemptNumber}: {args.Outcome.Exception.Message}";
                        
                        return default;
                    }
                });
            });
        }

        /// <summary>
        /// Adds a custom resilience pipeline using Microsoft.Extensions.Resilience.
        /// </summary>
        public IBackRunBuilder AddResilience(Action<ResiliencePipelineBuilder> configure)
        {
            if (builder.Services.Any(x => x.ImplementationType == typeof(BackRunResilienceMiddleware)))
            {
                return builder; 
            }
            
            var pipelineBuilder = new ResiliencePipelineBuilder();
            configure(pipelineBuilder);
        
            var pipeline = pipelineBuilder.Build();
        
            builder.Services.AddSingleton<IBackRunMiddleware>(new BackRunResilienceMiddleware(pipeline));
        
            return builder;
        }
    }
}