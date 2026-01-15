namespace BackRun.Abstractions
{
    public interface IBackRunJobHandler<in TPayload>
    {
        Task ExecuteAsync(
            TPayload payload,
            CancellationToken cancellationToken = default);
    }
}
