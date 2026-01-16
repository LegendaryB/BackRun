namespace BackRun.Abstractions
{
    /// <summary>
    /// Provides the primary entry point for managing background jobs, 
    /// including enqueuing new jobs and checking their current status.
    /// </summary>
    public interface IBackRunJobEngine
    {
        /// <summary>
        /// Enqueues a job for immediate or scheduled background execution.
        /// </summary>
        /// <typeparam name="TPayload">The type of the data payload.</typeparam>
        /// <typeparam name="THandler">The type of the handler that will process the payload.</typeparam>
        /// <param name="payload">The data to be processed by the handler.</param>
        /// <param name="options">Optional settings to override default queueing behavior (e.g., scheduling, custom queue name).</param>
        /// <param name="cancellationToken">A cancellation token for the enqueue operation itself.</param>
        /// <returns>The unique identifier assigned to the enqueued job.</returns>
        Task<Guid> EnqueueAsync<TPayload, THandler>(
            TPayload payload, 
            BackRunEnqueueOptions? options = null, 
            CancellationToken cancellationToken = default)
        
            where THandler : class, IBackRunJobHandler<TPayload>;

        /// <summary>
        /// Retrieves a job's current metadata and status from the underlying storage.
        /// </summary>
        /// <param name="id">The unique identifier of the job.</param>
        /// <param name="cancellationToken">A cancellation token for the retrieval operation.</param>
        /// <returns>The <see cref="BackRunJob"/> record if found; otherwise, null.</returns>
        Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
