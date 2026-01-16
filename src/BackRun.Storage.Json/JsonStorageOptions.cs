namespace BackRun.Storage.Json;

public class JsonStorageOptions
{
    /// <summary>
    /// The path where the JSON files will be stored.
    /// </summary>
    public string Path { get; set; } = "%APPDATA%/BackRunJobs";
}