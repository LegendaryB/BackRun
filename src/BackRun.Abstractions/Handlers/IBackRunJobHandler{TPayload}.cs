namespace BackRun.Abstractions
{
    /// <summary>
    /// Defines a handler for a specific background job payload.
    /// </summary>
    /// <typeparam name="TPayload">The type of data required to execute the job.</typeparam>
    public interface IBackRunJobHandler<in TPayload> : IBackRunJobHandler
    {
        /// <summary>
        /// Executes the background job logic.
        /// </summary>
        /// <param name="payload">The data associated with this job instance.</param>
        /// <param name="cancellationToken">A token that will be signaled if the application is shutting down or the job is canceled.</param>
        /// <returns>A task representing the asynchronous execution of the job.</returns>
        Task ExecuteAsync(
            TPayload payload,
            CancellationToken cancellationToken = default);
    }
}
