namespace BackRun.Abstractions
{
    public record BackRunEnqueueOptions
    {
        public DateTimeOffset? ScheduledAt { get; init; }
        
        public string? QueueName { get; init; }

        public static BackRunEnqueueOptions Delay(TimeSpan delay) =>
            new() { ScheduledAt = DateTimeOffset.UtcNow.Add(delay) };
    }
}
