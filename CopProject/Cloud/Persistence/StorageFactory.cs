// Author = Madhav P Nair
// Roll Number = 112301020

using System.Diagnostics;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;

namespace Cloud.Persistence;

/// <summary>
/// Creates IStorage instances for application modules
/// All modules share a Cosmosclient and each module gets its own container
/// </summary>
public static class StorageFactory
{
    // A single instance of CosmosClient is created and reused for all storage instances.

    // Delay construction so container-name mapping can be tested without Cosmos credentials.
    private static readonly Lazy<CosmosClient> s_cosmosClient = new(
    () => {
        Trace.TraceInformation(
            "[Cloud.Persistence][StorageFactory] Creating shared CosmosClient.");

        try
        {
            CosmosClient client = new CosmosClient(
                CosmosSettings.Endpoint,
                CosmosSettings.Key);

            Trace.TraceInformation(
                "[Cloud.Persistence][StorageFactory] Shared CosmosClient created successfully.");

            return client;
        }
        catch (Exception ex)
        {
            Trace.TraceError(
                $"[Cloud.Persistence][StorageFactory] Failed to create CosmosClient: {ex}");

            throw;
        }
    });

    /// <summary>
    /// This method asynchronously creates an IStorage instance for the specified module type if it doesn't exist .
    /// </summary>
    /// <param name="module"></param>
    /// <returns>IStorage instance ready to use</returns>
    public static Task<IStorage> CreateAsync(ModuleType module)
    {
        Trace.TraceInformation(
            $"[Cloud.Persistence][CreateAsync] Creating storage for module: {module}");

        return CreateAsync(module, s_cosmosClient);
    }

    // The overload lets tests provide a mocked CosmosClient without creating
    // a real client or connecting to a Cosmos account.
    internal static async Task<IStorage> CreateAsync(
        ModuleType module,
        Lazy<CosmosClient> cosmosClient)
    {
        Trace.TraceInformation(
            $"[Cloud.Persistence][CreateAsync] Initializing storage for module: {module}");

        try
        {
            string containerName = GetContainerName(module);

            Trace.TraceInformation(
                $"[Cloud.Persistence][CreateAsync] Resolved container: {containerName}");

            CosmosStorage storage = new CosmosStorage(
                cosmosClient.Value,
                CosmosSettings.DatabaseName,
                containerName);

            await storage.InitializeAsync();

            Trace.TraceInformation(
                $"[Cloud.Persistence][CreateAsync] Storage initialized successfully for module: {module}");

            return storage;
        }
        catch (Exception ex)
        {
            Trace.TraceError(
                $"[Cloud.Persistence][CreateAsync] Failed to create storage for module {module}: {ex}");

            throw;
        }
    }

    internal static string GetContainerName(ModuleType module)
    {
        Trace.TraceInformation(
            $"[Cloud.Persistence][GetContainerName] Resolving container for module: {module}");

        return module switch {
            ModuleType.Chat => "Chat",
            ModuleType.WhiteBoard => "Whiteboard",
            ModuleType.Networking => "Networking",
            ModuleType.FileSynchronizer => "FileSynchronizer",
            ModuleType.IncidentManagement => "IncidentManagement",
            ModuleType.Controller => "Controller",
            ModuleType.Insights => "Insights",
            ModuleType.LocationTracker => "LocationTracker",
            ModuleType.UX => "UX",
            ModuleType.ScreenShare => "ScreenShare",
            ModuleType.CrashReports => "CrashReports",
            ModuleType.CrashGroups => "CrashGroups",
            _ => throw new ArgumentException($"Unsupported module: {module}")
        };
    }
}
