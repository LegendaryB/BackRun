namespace BackRun.Abstractions
{
    public enum BackRunJobStatus
    {
        Queued,
        Scheduled,
        Running,
        Succeeded,
        Failed,
        Retrying
    }
}
