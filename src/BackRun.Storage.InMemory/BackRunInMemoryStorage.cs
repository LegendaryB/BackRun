using System.Collections.Concurrent;
using BackRun.Abstractions;

namespace BackRun.Storage.InMemory
{
    public class BackRunInMemoryStorage : IBackRunStorage
    {
        private readonly ConcurrentDictionary<Guid, BackRunJob> _jobs = new();

        public Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _jobs.TryGetValue(id, out var job);

            return Task.FromResult(job);
        }

        public Task StoreJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryAdd(job.Id, job))
                throw new InvalidOperationException($"Job with ID {job.Id} already exists.");

            return Task.CompletedTask;
        }

        public Task UpdateJobStatusAsync(
            Guid id,
            BackRunJobStatus status,
            string? error,
            CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryGetValue(id, out var job))
                throw new KeyNotFoundException($"Could not find job (id = {id}.");

            job.Status = status;
            job.Error = error;
            job.CompletedAt = status == BackRunJobStatus.Completed ?
                DateTimeOffset.UtcNow :
                null;

            return Task.CompletedTask;
        }
    }
}
