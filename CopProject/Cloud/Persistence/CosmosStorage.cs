using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Cloud.Persistence;
/// <summary>
/// Implements IStorage using Azure Cosmos DB
/// </summary>
internal class CosmosStorage : IStorage
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private readonly string _containerName;

    /* 
       the below functions create Cosmos query iterators instead of using LINQ queries
       this is done to allow mocking of the iterators for unit testing without a Cosmos account 
    */
    private readonly Func<FeedIterator<string>> _getAllKeysIterator; /* for GetAllKeys method */
    private readonly Func<FeedIterator<PersistenceItem>> _readAllEtagsIterator; /* for ReadAllEtags method */
    private readonly Func<int, FeedIterator<PersistenceItem>> _readLatestIterator; /* for ReadLatest method */
    private Container _container = null!;

    /// <summary>
    /// Initialises a new instance of CosmosStorage class
    /// </summary>
    /// <param name="client"></param>
    /// <param name="databaseName"></param>
    /// <param name="containerName"></param>
    public CosmosStorage(
        CosmosClient client,
        string databaseName,
        string containerName)
    {
        Trace.TraceInformation("[Cloud.Persistence][CosmosStorage] creating Cosmos storage instance.");
        _client = client;
        _databaseName = databaseName;
        _containerName = containerName;
        /* create iterators using actual Cosmos DB LINQ queries */
        _getAllKeysIterator = () => _container.GetItemLinqQueryable<PersistenceItem>()
            .Select(item => item.Id)
            .ToFeedIterator();
        _readAllEtagsIterator = () => _container.GetItemLinqQueryable<PersistenceItem>()
            .ToFeedIterator();
        _readLatestIterator = count => _container.GetItemLinqQueryable<PersistenceItem>()
            .OrderByDescending(item => item.TimeStamp)
            .Take(count)
            .ToFeedIterator();
    }

    // Allows storage behavior to be tested with a mocked container and no Cosmos account.
    internal CosmosStorage(
        Container container,
        Func<FeedIterator<string>> getAllKeysIterator,
        Func<FeedIterator<PersistenceItem>> readAllEtagsIterator,
        Func<int, FeedIterator<PersistenceItem>> readLatestIterator)
    {
        Trace.TraceInformation("[Cloud.Persistence][CosmosStorage] creating Cosmos storage instance with injected test dependencies.");
        _client = null!;
        _databaseName = string.Empty;
        _containerName = string.Empty;
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _getAllKeysIterator = getAllKeysIterator ?? throw new ArgumentNullException(nameof(getAllKeysIterator));
        _readAllEtagsIterator = readAllEtagsIterator ?? throw new ArgumentNullException(nameof(readAllEtagsIterator));
        _readLatestIterator = readLatestIterator ?? throw new ArgumentNullException(nameof(readLatestIterator));
    }

    /// <summary>
    /// This method initializes the Cosmos DB container for the storage instance.
    /// Call this method before doing the first storage operation
    /// </summary>
    /// <returns>Task</returns>
    internal async Task InitializeAsync()
    {
        Trace.TraceInformation("[Cloud.Persistence][InitializeAsync] initializing Cosmos DB database and container.");
        Database database =
            await _client.CreateDatabaseIfNotExistsAsync(
                _databaseName,
                ThroughputProperties.CreateManualThroughput(1000));

        ContainerResponse response =
            await database.CreateContainerIfNotExistsAsync(
                _containerName,
                "/id");

        _container = response.Container;
        Trace.TraceInformation("[Cloud.Persistence][InitializeAsync] Cosmos DB container initialized successfully.");

    }

    // All CRUD operations are done asynchronously.
    // IStorage interface methods are implemented here.


    /// <inheritdoc/>
    public async Task<bool> Create(string key, byte[] data)
    {
        Trace.TraceInformation("[Cloud.Persistence][Create] creating item.");
        PersistenceItem item = new PersistenceItem {
            Id = key,
            Data = data
        };

        try
        {
            await _container.CreateItemAsync(item, new PartitionKey(item.Id));
            Trace.TraceInformation($"[Cloud.Persistence][Create] item with ID '{key}' created successfully.");
            return true;
        }

        catch (CosmosException ex)
        {
            Trace.TraceError($"[Cloud.Persistence][Create] failed to create item with ID '{key}'. CosmosException: {ex.StatusCode} - {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, byte[] Data)> Read(string key)
    {
        Trace.TraceInformation($"[Cloud.Persistence][Read] reading item with ID '{key}'.");
        try
        {
            ItemResponse<PersistenceItem> response =
                await _container.ReadItemAsync<PersistenceItem>(
                    key,
                    new PartitionKey(key));

            Trace.TraceInformation($"[Cloud.Persistence][Read] item with ID '{key}' read successfully.");
            return (true, response.Resource.Data);
        }
        catch (CosmosException ex)
        {
            Trace.TraceError($"[Cloud.Persistence][Read] failed to read item with ID '{key}'.");
            Trace.TraceError($"[Cloud.Persistence][Read] CosmosException: {ex.StatusCode} - {ex.Message}");
            return (false, Array.Empty<byte>());
        }
    }

    /// <inheritdoc/>
    /* Concurrent updates are handled using _etag (version stamp) of the Cosmos DB item
       Each write changes the ETag
       For updating a key-value pair, one has to read the current ETag of the item */

    public async Task<(bool Success, string Etag)> ReadEtag(string key)
    {
        Trace.TraceInformation("[Cloud.Persistence][ReadEtag] reading item ETag.");
        try
        {
            ItemResponse<PersistenceItem> response =
                await _container.ReadItemAsync<PersistenceItem>(
                    key,
                    new PartitionKey(key));
            Trace.TraceInformation("[Cloud.Persistence][ReadEtag] item ETag read successfully.");
            return (true, response.ETag);
        }
        catch (CosmosException ex)
        {
            Trace.TraceError($"[Cloud.Persistence][ReadEtag] failed to read item ETag with ID '{key}'. CosmosException: {ex.StatusCode} - {ex.Message}");
            return (false, string.Empty);
        }
    }

    /// <inheritdoc/>

    /* One has to correctly match the ETag he read earlier with the current ETag of the item
       Only one of the concurrent writers who read the same _etag succeeds in Update operation
       This is because the winner changes the ETag by writing which makes others' ETags stale */
    public async Task<bool> Update(string key, byte[] data, string etag)
    {
        Trace.TraceInformation("[Cloud.Persistence][Update] updating item with ETag concurrency check.");

        PersistenceItem item = new PersistenceItem {
            Id = key,
            Data = data
        };

        try
        {
            await _container.ReplaceItemAsync(
                item,
                key,
                new PartitionKey(key),
                new ItemRequestOptions {
                    IfMatchEtag = etag
                });

            Trace.TraceInformation("[Cloud.Persistence][Update] item updated successfully.");
            return true;

        }
        catch (CosmosException ex)
        {
            // The ETag does not match, so the item has been modified by another process
            if (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                Trace.TraceWarning($"[Cloud.Persistence][Update] update rejected because the ETag did not match for item with ID '{key}'.");
            }
            else
            {
                Trace.TraceError($"[Cloud.Persistence][Update] failed to update item with ID '{key}'. CosmosException: {ex.StatusCode} - {ex.Message}");
            }
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Delete(string key)
    {
        Trace.TraceInformation("[Cloud.Persistence][Delete] deleting item.");
        try
        {
            await _container.DeleteItemAsync<PersistenceItem>(
                key,
                new PartitionKey(key));
            Trace.TraceInformation("[Cloud.Persistence][Delete] item deleted successfully.");
            return true;
        }
        catch (CosmosException ex)
        {
            Trace.TraceError($"[Cloud.Persistence][Delete] failed to delete item with ID '{key}'. CosmosException: {ex.StatusCode} - {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CreateAll(Dictionary<string, byte[]> items)
    {
        Trace.TraceInformation($"[Cloud.Persistence][CreateAll] creating {items.Count} items.");
        foreach (KeyValuePair<string, byte[]> item in items)
        {
            if (!await Create(item.Key, item.Value))
            {
                Trace.TraceError($"[Cloud.Persistence][CreateAll] bulk creation stopped because an item with ID '{item.Key}' could not be created.");
                return false;
            }
        }
        Trace.TraceInformation("[Cloud.Persistence][CreateAll] all items created successfully.");
        return true;
    }

    /// <inheritdoc/>
    // Empty array is returned even if one read fails.
    // All other bulk CRUD operations are atomic like this
    public async Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
    {
        Trace.TraceInformation($"[Cloud.Persistence][ReadAll] reading {keys.Length} items.");
        Dictionary<string, byte[]> result = [];
        foreach (string key in keys)
        {
            (bool success, byte[] data) = await Read(key);
            if (success)
            {
                result[key] = data;
            }
            else
            {
                Trace.TraceError($"[Cloud.Persistence][ReadAll] bulk read stopped because an item with ID '{key}' could not be read.");
                return (false, new Dictionary<string, byte[]>());
            }
        }
        Trace.TraceInformation("[Cloud.Persistence][ReadAll] all items read successfully.");
        return (true, result);
    }

    /// <inheritdoc/>
    public async Task<(bool Success, List<string>)> GetAllKeys()
    {
        Trace.TraceInformation("[Cloud.Persistence][GetAllKeys] retrieving all item keys.");
        List<string> keys = [];

        try
        {
            using FeedIterator<string> iterator = _getAllKeysIterator();

            while (iterator.HasMoreResults)
            {
                FeedResponse<string> page = await iterator.ReadNextAsync();
                keys.AddRange(page);
            }

            Trace.TraceInformation($"[Cloud.Persistence][GetAllKeys] retrieved {keys.Count} keys.");
            return (true, keys);
        }
        catch (CosmosException ex)
        {
            Trace.TraceError($"[Cloud.Persistence][GetAllKeys] failed to retrieve item keys. CosmosException: {ex.StatusCode} - {ex.Message}");
            return (false, new List<string>());
        }
    }

    /// <inheritdoc/>
    /* this will be helpful when all the items are to be updated */
    public async Task<(bool Success, Dictionary<string, string>)> ReadAllEtags()
    {
        Trace.TraceInformation("[Cloud.Persistence][ReadAllEtags] retrieving all item ETags.");
        Dictionary<string, string> etags = [];

        try
        {
            using FeedIterator<PersistenceItem> iterator = _readAllEtagsIterator();

            while (iterator.HasMoreResults)
            {
                FeedResponse<PersistenceItem> page = await iterator.ReadNextAsync();

                foreach (PersistenceItem item in page)
                {
                    string? etag = item.Etag;

                    if (etag is null)
                    {
                        Trace.TraceError("[Cloud.Persistence][ReadAllEtags] encountered an item with a null ETag.");
                        return (false, new Dictionary<string, string>());
                    }
                    else
                    {
                        etags[item.Id] = item.Etag;
                    }
                }
            }

            Trace.TraceInformation($"[Cloud.Persistence][ReadAllEtags] retrieved {etags.Count} ETags.");
            return (true, etags);
        }
        catch (CosmosException ex)
        {
            Trace.TraceError($"[Cloud.Persistence][ReadAllEtags] failed to retrieve item ETags. CosmosException: {ex.StatusCode} - {ex.Message}");
            return (false, new Dictionary<string, string>());
        }
    }

    /// <inheritdoc/>
    /* reads ETags of the items specified by the keys in the array.
       A dictionary of Id : Etag of each item is returned
       UpdateAll method is called only after getting the ETags of all the required items
    */
    public async Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys)
    {
        Trace.TraceInformation($"[Cloud.Persistence][ReadEtags] retrieving ETags for {keys.Length} items.");
        Dictionary<string, string> etags = new Dictionary<string, string>();
        foreach (string key in keys)
        {
            (bool success, string etag) = await ReadEtag(key);
            if (success)
            {
                etags[key] = etag;
            }
            else
            {
                Trace.TraceError($"[Cloud.Persistence][ReadEtags] failed to retrieve ETag for item with ID '{key}'.");
                return (false, new Dictionary<string, string>());
            }
        }
        Trace.TraceInformation($"[Cloud.Persistence][ReadEtags] retrieved {etags.Count} ETags.");
        return (true, etags);
    }

    /// <inheritdoc/>
    /* 
     Call UpdateAll method with the key-value pairs to be updated and the Etags of all required items.
     This should be obtained by calling ReadEtags method.
    */
    public async Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags)
    {
        Trace.TraceInformation($"[Cloud.Persistence][UpdateAll] updating {items.Count} items.");
        foreach (string key in items.Keys)
        {
            if (!etags.ContainsKey(key))
            {
                Trace.TraceWarning($"[Cloud.Persistence][UpdateAll] an ETag is missing for item with ID '{key}'.");
                return false;
            }
        }

        foreach (KeyValuePair<string, byte[]> item in items)
        {
            bool success = await Update(
                item.Key,
                item.Value,
                etags[item.Key]);

            if (!success)
            {
                Trace.TraceError($"[Cloud.Persistence][UpdateAll] bulk update stopped because item with ID '{item.Key}' could not be updated.");
                return false;
            }
        }

        Trace.TraceInformation("[Cloud.Persistence][UpdateAll] all items updated successfully.");
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAll(string[] keys)
    {
        Trace.TraceInformation($"[Cloud.Persistence][DeleteAll] deleting {keys.Length} items.");
        foreach (string key in keys)
        {
            if (!await Delete(key))
            {
                Trace.TraceError($"[Cloud.Persistence][DeleteAll] bulk deletion stopped because item with ID '{key}' could not be deleted");
                return false;
            }
        }
        Trace.TraceInformation("[Cloud.Persistence][DeleteAll] all items deleted successfully.");
        return true;
    }

    /// <inheritdoc/>
    /*  Read the latest n items from the container. Useful for retrieving recent records
        eg: read the latest 10 incidents
    */
    public async Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int count)
    {
        Trace.TraceInformation($"[Cloud.Persistence][ReadLatest] retrieving the latest {count} items");
        Dictionary<string, byte[]> items = [];

        /* return false and empty dictionary if count is invalid */
        if (count <= 0)
        {
            Trace.TraceWarning("[Cloud.Persistence][ReadLatest] count must be greater than zero.");
            return (false, items);
        }

        try
        {
            /* retrieve the latest 'count' items ordered by their last modification time (_ts) */
            using FeedIterator<PersistenceItem> iterator = _readLatestIterator(count);

            while (iterator.HasMoreResults)
            {
                FeedResponse<PersistenceItem> page = await iterator.ReadNextAsync();

                foreach (PersistenceItem item in page)
                {
                    items[item.Id] = item.Data;
                }
            }

            Trace.TraceInformation($"[Cloud.Persistence][ReadLatest] retrieved {items.Count} items");
            return (true, items);
        }
        catch (CosmosException ex)
        {
            Trace.TraceError($"[Cloud.Persistence][ReadLatest] failed to retrieve the latest items. CosmosException: {ex.StatusCode} - {ex.Message}");
            return (false, []); /* return false and empty dict in case of exceptions */
        }
    }

}
