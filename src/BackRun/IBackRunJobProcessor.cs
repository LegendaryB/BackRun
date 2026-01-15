using BackRun.Abstractions;

namespace BackRun
{
    public interface IBackRunJobProcessor
    {
        Task ProcessJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default);
    }
}
