using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using BackRun.Abstractions;
using static BackRun.Logging.Engine;

namespace BackRun
{
    public class BackRunJobEngine :
        BackgroundService,
        IBackRunJobEngine
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

        private readonly ILogger<BackRunJobEngine> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBackRunJobProcessor _processor;
        private readonly BackRunOptions _options;

        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentBag<Task> _runningTasks = [];

        public BackRunJobEngine(
            ILogger<BackRunJobEngine> logger,
            IServiceProvider serviceProvider,
            IBackRunJobProcessor processor,
            IOptions<BackRunOptions> options)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _processor = processor;
            _options = options.Value;

            _semaphore = new SemaphoreSlim(_options.MaxDegreeOfParallelism);
        }

        public async Task<Guid> EnqueueAsync<TPayload, THandler>(
            TPayload payload,
            BackRunEnqueueOptions? options = null,
            CancellationToken cancellationToken = default)

            where THandler : class, IBackRunJobHandler<TPayload>
        {
            ArgumentNullException.ThrowIfNull(payload);

            _logger.LogInformation(
                "Enqueuing job with payload (HandlerType = {HandlerType}, PayloadType = {PayloadType}).",
                typeof(THandler).AssemblyQualifiedName,
                typeof(TPayload).FullName);

            var handlerTypeQualifiedName = typeof(THandler).AssemblyQualifiedName;

            if (string.IsNullOrWhiteSpace(handlerTypeQualifiedName))
            {
                _logger.LogHandlerHasNoValidAssemblyQualifiedName(typeof(THandler).FullName);

                throw new InvalidOperationException(
                    $"Handler type {typeof(THandler).FullName} has no {nameof(Type.AssemblyQualifiedName)}.");
            }

            var storedJob = await UseStorageAsync(async storage =>
            {
                var job = CreateJob(
                    handlerTypeQualifiedName,
                    payload,
                    options);

                await storage.StoreJobAsync(
                    job,
                    cancellationToken);

                return job;
            });

            _logger.LogJobStored(storedJob.Id);

            if (!_channel.Writer.TryWrite(storedJob.Id))
            {
                throw new InvalidOperationException(
                    "Failed to enqueue job id into processing channel.");
            }

            _logger.LogDebug(
                "Job enqueued for processing (JobId = {JobId}).",
                storedJob.Id);

            return storedJob.Id;
        }

        public async Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Getting job from storage (JobId = {JobId}).",
                id);

            var job = await UseStorageAsync(storage =>
                storage.GetJobAsync(id, cancellationToken));

            if (job == null)
            {
                _logger.LogWarning(
                    "Job not found in storage (JobId = {JobId}).",
                    id);

                return null;
            }

            _logger.LogDebug(
                "Successfully retrieved job (JobId = {JobId}, Status = {Status}).",
                job.Id,
                job.Status);

            return job;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackRunJobEngine started.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var jobId = await _channel.Reader.ReadAsync(cancellationToken);
                await _semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    using var scope = _logger.BeginScope("JobId: {JobId}", jobId);

                    try
                    {
                        _logger.LogDebug(
                            "Dequeued job for processing (JobId = {JobId}).",
                            jobId);

                        var job = await GetJobAsync(
                            jobId,
                            cancellationToken);

                        if (job == null)
                        {
                            _logger.LogWarning(
                                "Job was not found in storage (JobId = {JobId}).",
                                jobId);

                            return;
                        }

                        _logger.LogInformation(
                            "Starting processing of job (JobId = {JobId}).",
                            jobId);

                        try
                        {
                            await _processor.ProcessJobAsync(
                                job,
                                cancellationToken);

                            // Mark as completed
                            await UseStorageAsync(storage =>
                                storage.UpdateJobStatusAsync(job.Id, BackRunJobStatus.Succeeded, null,
                                    cancellationToken));

                            _logger.LogInformation(
                                "Successfully completed processing of job (JobId = {JobId}).",
                                jobId);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogError(
                                ex,
                                "Job failed (JobId = {JobId}).",
                                jobId);

                            await UseStorageAsync(storage =>
                                storage.UpdateJobStatusAsync(
                                    job.Id,
                                    BackRunJobStatus.Failed,
                                    ex.Message,
                                    cancellationToken));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning(
                            "Processing of job was canceled (JobId = {JobId}).",
                            jobId);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, cancellationToken);

                _runningTasks.Add(task);
            }

            _logger.LogInformation("Waiting for all running jobs to complete...");

            await Task.WhenAll([.. _runningTasks]);

            _logger.LogInformation("All remaining jobs completed.");
            _logger.LogInformation("BackRunJobEngine stopped.");
        }

        private BackRunJob CreateJob<TPayload>(
            string handlerType,
            TPayload payload,
            BackRunEnqueueOptions? options)
        {
            var serializedPayload = JsonSerializer.Serialize(payload);

            var job = new BackRunJob
            {
                Id = Guid.NewGuid(),
                HandlerType = handlerType,
                PayloadJson = serializedPayload,
                Status = BackRunJobStatus.Queued,
                CreatedAt = DateTime.UtcNow,
            };

            _logger.LogDebug(
                "Created job (JobId = {JobId}, HandlerType = {HandlerType}.",
                job.Id,
                handlerType);

            return job;
        }

        private async Task<TResult> UseStorageAsync<TResult>(Func<IBackRunStorage, Task<TResult>> action)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();

            _logger.LogDebug(
                "Executing storage action ({StorageType}).",
                storage.GetType().FullName);

            return await action(storage);
        }

        private async Task UseStorageAsync(Func<IBackRunStorage, Task> action)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IBackRunStorage>();

            _logger.LogDebug(
                "Executing storage action ({StorageType}).",
                storage.GetType().FullName);

            await action(storage);
        }
    }
}
