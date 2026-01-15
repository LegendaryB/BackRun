namespace BackRun.Abstractions;

public interface IBackRunMiddleware
{
    Task ExecuteAsync(
        BackRunJob job,
        Func<BackRunJob, Task> next,
        CancellationToken cancellationToken = default);
}