using BackRun.Abstractions;
using JsonFlatFileDataStore;

namespace BackRun.Storage.JsonFlatFileDataStore
{
    public class BackRunJsonFlatFileDataStorage : IBackRunStorage
    {
        private readonly DataStore _store = new DataStore("BackRun.json");

        public async Task StoreJobAsync(
            BackRunJob job,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(job);

            var store = new DataStore("BackRun.json");
            var jobCollection = store.GetCollection<BackRunJob>();

            await jobCollection.InsertOneAsync(job);
        }

        public Task UpdateJobStatusAsync(
            Guid id,
            BackRunJobStatus status,
            string? error,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BackRunJob?> GetJobAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
