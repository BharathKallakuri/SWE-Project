using Microsoft.Azure.Cosmos;
using Persistence;

public class CosmosStorage : IStorage
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;
    private Container _container = null!;

    public CosmosStorage(
        CosmosClient client,
        string databaseName,
        string containerName)
    {
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
    }

    // This method initializes the Cosmos DB container for the storage instance.
    public async Task InitializeAsync()
    {
        Database database =
            await _client.CreateDatabaseIfNotExistsAsync(_databaseName);

        ContainerResponse response =
            await database.CreateContainerIfNotExistsAsync(
                _containerName,
                "/id");

        _container = response.Container;

    }

    // All CRUD operations are done asynchronously.
    // IStorage interface methods are implemented here.
    public async Task Create(string key, byte[] data)
    {
        PersistenceItem item = new PersistenceItem
        {
            Id = key,
            Data = data
        };


        await _container.CreateItemAsync(item, new PartitionKey(item.Id));
    }

    public async Task<byte[]> Read(string key)
    {
        ItemResponse<PersistenceItem> response =
            await _container.ReadItemAsync<PersistenceItem>(
                key,
                new PartitionKey(key));

        return response.Resource.Data;
    }

    public async Task Update(string key, byte[] data)
    {
        PersistenceItem item = new PersistenceItem
        {
            Id = key,
            Data = data
        };

        await _container.ReplaceItemAsync(
            item,
            key,
            new PartitionKey(key));
    }


    public async Task Delete(string key)
    {
        await _container.DeleteItemAsync<PersistenceItem>(
            key,
            new PartitionKey(key));
    }

    
}