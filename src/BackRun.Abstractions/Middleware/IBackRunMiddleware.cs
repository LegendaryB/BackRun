namespace BackRun.Abstractions;

/// <summary>
/// Defines a middleware that can intercept and wrap the execution of a background job.
/// </summary>
public interface IBackRunMiddleware
{
    /// <summary>
    /// Wraps the execution of a job.
    /// </summary>
    /// <param name="job">The job being processed, containing metadata and status.</param>
    /// <param name="next">A delegate representing the next middleware in the pipeline or the final job handler.</param>
    /// <param name="cancellationToken">The cancellation token for the current execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// You must call 'await next(job)' to continue the pipeline, 
    /// unless you intentionally want to short-circuit the execution.
    /// </remarks>
    Task ExecuteAsync(
        BackRunJob job,
        Func<BackRunJob, Task> next,
        CancellationToken cancellationToken = default);
}