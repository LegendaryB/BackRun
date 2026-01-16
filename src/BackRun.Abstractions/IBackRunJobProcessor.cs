namespace BackRun.Abstractions
{
    /// <summary>
    /// Orchestrates the execution of a background job by building 
    /// and running it through the middleware pipeline.
    /// </summary>
    public interface IBackRunJobProcessor
    {
        /// <summary>
        /// Processes a job by executing all registered middlewares and finally invoking the job handler.
        /// </summary>
        /// <param name="job">The job record to be processed.</param>
        /// <param name="cancellationToken">A cancellation token for the duration of the job execution.</param>
        /// <returns>A task representing the completion of the processing pipeline.</returns>
        /// <remarks>
        /// This method is typically called by the <see cref="IBackRunJobEngine"/> background worker.
        /// </remarks>
        Task ProcessJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default);
    }
}
