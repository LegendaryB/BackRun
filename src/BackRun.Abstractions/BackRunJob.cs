namespace BackRun.Abstractions
{
    public class BackRunJob
    {
        public Guid Id { get; init; }

        public required string HandlerType { get; init; }

        public string PayloadJson { get; set; } = null!;

        public BackRunJobStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? CompletedAt { get; set; }

        public string? Error { get; set; }
    }
}
