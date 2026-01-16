namespace BackRun.Abstractions
{
    /// <summary>
    /// Defines the contract for persisting and retrieving background job data.
    /// Implement this interface to support different storage backends (e.g., SQL, Redis, NoSQL).
    /// </summary>
    public interface IBackRunStorage
    {
        /// <summary>
        /// Persists a new job record to the storage medium.
        /// </summary>
        /// <param name="job">The job record to store.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        Task StoreJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a specific job by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the job.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>The <see cref="BackRunJob"/> if found; otherwise, null.</returns>
        Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates all mutable properties of an existing job (e.g., Status, RetryCount, FinishedAt).
        /// </summary>
        /// <param name="job">The job record with updated values.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        Task UpdateJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all jobs that are currently in a 'Running' or 'Queued' state.
        /// Used during engine startup to recover jobs interrupted by an application crash or restart.
        /// </summary>
        /// <param name="batchSize">The maximum number of jobs to retrieve in this call.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A collection of jobs requiring recovery.</returns>
        Task<IEnumerable<BackRunJob>> GetRunningJobsAsync(
            int batchSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves jobs that are 'Scheduled' and whose 'ScheduledAt' time has passed.
        /// </summary>
        /// <param name="now">The current reference time (usually UTC).</param>
        /// <param name="batchSize">The maximum number of jobs to retrieve in this call.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A collection of jobs ready to be moved to the active queue.</returns>
        Task<IEnumerable<BackRunJob>> GetPendingScheduledJobsAsync(
            DateTimeOffset now,
            int batchSize,
            CancellationToken cancellationToken = default);

        /* /// <summary>
        /// Removes completed or failed jobs that are older than the specified timestamp.
        /// </summary>
        /// <param name="olderThan">The cutoff time for deletion.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        Task DeleteOldJobsAsync(
            DateTimeOffset olderThan,
            CancellationToken cancellationToken = default); */
    }
}
