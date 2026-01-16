using BackRun.Abstractions;

namespace BackRun.Middleware;

internal class BackRunStatusMiddleware(IBackRunStorage storage) : IBackRunMiddleware
{
    public async Task ExecuteAsync(
        BackRunJob job,
        Func<BackRunJob, Task> next,
        CancellationToken cancellationToken = default)
    {
        job.Status = BackRunJobStatus.Running;
        
        await storage.UpdateJobAsync(
            job,
            cancellationToken);

        try
        {
            await next(job);

            job.Status = BackRunJobStatus.Succeeded;
            job.CompletedAt = DateTimeOffset.UtcNow;
            
            await storage.UpdateJobAsync(
                job,
                cancellationToken);
        }
        catch (Exception ex)
        {
            job.Status = BackRunJobStatus.Failed;
            job.LastError = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            
            await storage.UpdateJobAsync(
                job,
                cancellationToken);
            
            throw; 
        }
    }
}