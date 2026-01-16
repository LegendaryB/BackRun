using System.Collections.Concurrent;
using BackRun.Abstractions;

namespace BackRun.Storage.InMemory
{
    public class BackRunInMemoryStorage : IBackRunStorage
    {
        private readonly ConcurrentDictionary<Guid, BackRunJob> _jobs = new();

        public Task StoreJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default)
        {
            _jobs[job.Id] = job;
            return Task.CompletedTask;
        }

        public Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _jobs.TryGetValue(id, out var job);
            return Task.FromResult(job);
        }

        public Task UpdateJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default)
        {
            _jobs[job.Id] = job;
            
            return Task.CompletedTask;
        }

        public Task<IEnumerable<BackRunJob>> GetRunningJobsAsync(
            CancellationToken cancellationToken = default)
        {
            var orphaned = _jobs.Values.Where(job => 
                job.Status is BackRunJobStatus.Running or BackRunJobStatus.Queued);
            
            return Task.FromResult(orphaned);
        }

        public Task<IEnumerable<BackRunJob>> GetPendingScheduledJobsAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            var pending = _jobs.Values.Where(job => 
                job.Status == BackRunJobStatus.Scheduled && 
                job.ScheduledAt <= now);
            
            return Task.FromResult(pending);
        }

        public Task DeleteOldJobsAsync(
            DateTimeOffset olderThan,
            CancellationToken cancellationToken = default)
        {
            var toRemove = _jobs.Values.Where(job => 
                job.Status is BackRunJobStatus.Succeeded or BackRunJobStatus.Failed && 
                job.CompletedAt < olderThan);

            foreach (var job in toRemove)
                _jobs.TryRemove(job.Id, out _);
        
            return Task.CompletedTask;
        }
    }
}
