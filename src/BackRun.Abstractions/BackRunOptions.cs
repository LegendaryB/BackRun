namespace BackRun.Abstractions
{
    public class BackRunOptions
    {
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        
        /// <summary>
        /// How many jobs to fetch from storage in a single polling/recovery tick.
        /// This prevents memory exhaustion.
        /// </summary>
        public int PollingBatchSize { get; set; } = 100;
    }
}
