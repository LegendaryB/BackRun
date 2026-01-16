namespace BackRun.Abstractions
{
    public record BackRunJob
    {
        public required Guid Id { get; init; } = Guid.NewGuid();

        public required string HandlerType { get; init; }

        public required string PayloadType { get; init; }
        
        public required string PayloadJson { get; init; }
        
        public BackRunJobStatus Status { get; set; } = BackRunJobStatus.Queued;

        public string QueueName { get; init; } = "default";
        
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ScheduledAt { get; init; }

        public int RetryCount { get; set; }
        
        public string? LastError { get; set; }
        
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
