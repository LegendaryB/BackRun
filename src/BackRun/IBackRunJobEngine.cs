using BackRun.Abstractions;

namespace BackRun
{
    public interface IBackRunJobEngine
    {
        Task<Guid> EnqueueAsync<TPayload, THandler>(
            TPayload payload,
            BackRunEnqueueOptions? options = null,
            CancellationToken cancellationToken = default)

            where THandler : class, IBackRunJobHandler<TPayload>;

        Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
