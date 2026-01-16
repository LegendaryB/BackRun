using System.Text.Json;
using BackRun.Abstractions;

namespace BackRun.Storage.Json;

internal sealed class JsonFlatFileStorage : IBackRunStorage
{
    private readonly string _storagePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    public JsonFlatFileStorage(string storagePath)
    {
        _storagePath = storagePath ??
            throw new ArgumentNullException(nameof(storagePath));
        
        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }
    
    public async Task StoreJobAsync(
        BackRunJob job,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(job.Id);
        
        var json = JsonSerializer.Serialize(
            job,
            JsonOptions);
        
        await File.WriteAllTextAsync(
            filePath, 
            json,
            cancellationToken);
    }

    public async Task<BackRunJob?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(jobId);
        
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(
            filePath,
            cancellationToken);
        
        return JsonSerializer.Deserialize<BackRunJob>(
            json,
            JsonOptions);
    }

    public async Task UpdateJobAsync(
        BackRunJob job,
        CancellationToken cancellationToken = default)
    {
        await StoreJobAsync(
            job,
            cancellationToken);
    }

    public async Task<IEnumerable<BackRunJob>> GetRunningJobsAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await GetFilteredJobsAsync(
            job => job.Status is BackRunJobStatus.Running or BackRunJobStatus.Queued,
            batchSize,
            cancellationToken);
    }

    public async Task<IEnumerable<BackRunJob>> GetPendingScheduledJobsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await GetFilteredJobsAsync(
            job => job.Status == BackRunJobStatus.Scheduled && job.ScheduledAt <= now,
            batchSize,
            cancellationToken);
    }

    private string GetFilePath(Guid id) => Path.Combine(
        _storagePath,
        $"{id}.json");

    private async Task<List<BackRunJob>> GetFilteredJobsAsync(
        Func<BackRunJob, bool> filter,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_storagePath))
            return [];

        var files = Directory.GetFiles(_storagePath, "*.json");
        var results = new List<BackRunJob>();

        foreach (var file in files)
        {
            if (results.Count >= batchSize) break;

            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var job = JsonSerializer.Deserialize<BackRunJob>(json, JsonOptions);
                
                if (job != null && filter(job))
                {
                    results.Add(job);
                }
            }
            catch (IOException) {}
        }

        return results;
    }
}