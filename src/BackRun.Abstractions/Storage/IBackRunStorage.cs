namespace BackRun.Abstractions
{
    public interface IBackRunStorage
    {
        Task StoreJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default);

        Task UpdateJobStatusAsync(
            Guid id,
            BackRunJobStatus status,
            string? error,
            CancellationToken cancellationToken = default);

        Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
