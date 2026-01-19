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
            if (job.Status != BackRunJobStatus.Retrying)
            {
                job.Status = BackRunJobStatus.Failed;
                job.CompletedAt = DateTimeOffset.UtcNow;
            }
            
            job.LastError = ex.Message;
            
            await storage.UpdateJobAsync(
                job,
                cancellationToken);
            
            throw; 
        }
    }
}