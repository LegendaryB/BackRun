using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using BackRun.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static BackRun.Logging.BackRunJobEngineLogMessages;

namespace BackRun;

internal sealed class BackRunJobEngine(
    ILogger<BackRunJobEngine> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<BackRunOptions> options) : BackgroundService, IBackRunJobEngine
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();
    private readonly SemaphoreSlim _semaphore = new(options.Value.MaxDegreeOfParallelism);
    private readonly ConcurrentBag<Task> _runningTasks = [];
    private readonly BackRunOptions _options = options.Value;

    public async Task<Guid> EnqueueAsync<TPayload, THandler>(
        TPayload payload, 
        BackRunEnqueueOptions? enqueueOptions = null, 
        CancellationToken cancellationToken = default)
    
        where THandler : class, IBackRunJobHandler<TPayload>
    {
        var job = new BackRunJob
        {
            Id = Guid.NewGuid(),
            HandlerType = typeof(THandler).AssemblyQualifiedName!,
            PayloadType = typeof(TPayload).AssemblyQualifiedName!,
            PayloadJson = JsonSerializer.Serialize(payload),
            QueueName = enqueueOptions?.QueueName ?? "default",
            ScheduledAt = enqueueOptions?.ScheduledAt,
            Status = enqueueOptions?.ScheduledAt > DateTimeOffset.UtcNow 
                ? BackRunJobStatus.Scheduled 
                : BackRunJobStatus.Queued
        };

        await using var scope = scopeFactory.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();
        
        await storage.StoreJobAsync(
            job,
            cancellationToken);

        if (job.Status == BackRunJobStatus.Queued)
        {
            await _channel.Writer.WriteAsync(
                job.Id,
                cancellationToken);
        }

        return job.Id;
    }

    public async Task<BackRunJob?> GetJobAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();
        
        return await storage.GetJobAsync(
            id,
            cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogJobEngineStarted(_options.MaxDegreeOfParallelism);

        await RecoverOrphanedJobsAsync(stoppingToken);

        _ = RunSchedulerPollerAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _channel.Reader.ReadAsync(stoppingToken);
                await _semaphore.WaitAsync(stoppingToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessSingleJobAsync(jobId, stoppingToken);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, stoppingToken);

                _runningTasks.Add(task);
                CleanCompletedTasks();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogJobEngineMainLoopException(ex);
            }
        }

        logger.LogJobEngineStopped(_runningTasks.Count);
        
        await Task.WhenAll([.. _runningTasks]);
    }

    private async Task ProcessSingleJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();
        var processor = scope.ServiceProvider.GetRequiredService<IBackRunJobProcessor>();

        var job = await storage.GetJobAsync(
            jobId,
            cancellationToken);
        
        if (job is null)
            return;

        try
        {
            await processor.ProcessJobAsync(
                job,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogJobFinishedWithException(
                ex,
                jobId);
        }
    }

    private async Task RecoverOrphanedJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();
            
            var orphanedJobs = await storage.GetRunningJobsAsync(
                _options.PollingBatchSize,
                cancellationToken);
            
            foreach (var job in orphanedJobs)
            {
                logger.LogRecoveringOrphanedJob(
                    job.Id,
                    job.Status);
                
                await _channel.Writer.WriteAsync(
                    job.Id,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogRecoveringOrphanedJobFailed(ex);
        }
    }

    private async Task RunSchedulerPollerAsync(CancellationToken cancellationToken = default)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5)); 
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();
                
                var readyJobs = await storage.GetPendingScheduledJobsAsync(
                    DateTimeOffset.UtcNow,
                    _options.PollingBatchSize,
                    cancellationToken);
                
                foreach (var job in readyJobs)
                {
                    job.Status = BackRunJobStatus.Queued;
                    
                    await storage.UpdateJobAsync(
                        job,
                        cancellationToken);
                    
                    await _channel.Writer.WriteAsync(
                        job.Id,
                        cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogExceptionInSchedulerPoller(ex);
            }
        }
    }

    private void CleanCompletedTasks()
    {
        if (_runningTasks.Count <= 100)
            return;
        
        var completed = _runningTasks
            .Where(t => t.IsCompleted)
            .ToList();
        
        foreach (var t in completed)
            _runningTasks.TryTake(out _);
    }
}