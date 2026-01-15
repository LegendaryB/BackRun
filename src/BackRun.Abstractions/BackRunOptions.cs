namespace BackRun.Abstractions
{
    public class BackRunOptions
    {
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    }
}
