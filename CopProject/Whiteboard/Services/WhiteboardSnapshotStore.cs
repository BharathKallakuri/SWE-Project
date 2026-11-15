/// Author : Karthik Krishna M
/// Roll Number : 112301014
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Cloud.Persistence;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Services;

public sealed class WhiteboardSnapshotStore
{
    private readonly IStorage _storage;

    public WhiteboardSnapshotStore(IStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
    }

    public static async Task<WhiteboardSnapshotStore> CreateAsync()
    {
        IStorage storage = await StorageFactory.CreateAsync(ModuleType.WhiteBoard);
        return new WhiteboardSnapshotStore(storage);
    }

    private static string GetKeyPrefix(string sessionId)
    {
        return $"session:{sessionId}:snapshot:";
    }

    private static string GetSnapshotKey(string sessionId, string snapshotId)
    {
        return $"{GetKeyPrefix(sessionId)}{snapshotId}";
    }

    public async Task<WhiteboardSnapshotSummary> SaveSnapshotAsync(string sessionId, string userId, IEnumerable<ItemDto> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(items);

       var snapshot = new WhiteboardSnapshot
       {
            SessionId = sessionId,
            SnapshotId = Guid.NewGuid().ToString("N"),
            SavedBy = userId,
            SavedAt = DateTimeOffset.UtcNow,
            Items = items.ToList()
       };

       byte[] data = JsonSerializer.SerializeToUtf8Bytes(snapshot);
       string key = GetSnapshotKey(sessionId, snapshot.SnapshotId);

       Trace.TraceInformation($"[Whiteboard][WhiteboardSnapshotStore][SaveSnapshotAsync][Information] " +
        $"Saving snapshot {snapshot.SnapshotId} with {snapshot.Items.Count} items");
        
       if(!await _storage.Create(key, data))
       {
            Trace.TraceError($"[Whiteboard][WhiteboardSnapshotStore][SaveSnapshotAsync][Error] " +
            $"Cloud failed to save snapshot {snapshot.SnapshotId}");

            throw new InvalidOperationException("Cloud failed to save whiteboard snapshot");
       }

       Trace.TraceInformation($"[Whiteboard][WhiteboardSnapshotStore][SaveSnapshotAsync][Information] " +
        $"Saved snapshot {snapshot.SnapshotId}.");

       return new WhiteboardSnapshotSummary
       {
            SnapshotId = snapshot.SnapshotId,
            SavedBy = snapshot.SavedBy,
            SavedAt = snapshot.SavedAt
       };
    }

    public async Task<List<WhiteboardSnapshotSummary>> ListAsync(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        Trace.TraceInformation("[Whiteboard][WhiteboardSnapshotStore][ListAsync][Information] Listing snapshots");

        var (success, keys) = await _storage.GetAllKeys();

        if(!success)
        {
            Trace.TraceError("[Whiteboard][WhiteboardSnapshotStore][ListAsync][Error] Cloud failed to list snapshot keys");

            throw new InvalidOperationException("Cloud failed to list whiteboard snapshots");
        }

        string prefix = GetKeyPrefix(sessionId);
        var snapshots = new List<WhiteboardSnapshotSummary>();

        foreach(string key in keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            var (readSuccess, data) = await _storage.Read(key);

            if(!readSuccess)
            {
                Trace.TraceWarning("[Whiteboard][WhiteboardSnapshotStore][ListAsync][Warning] Could not read a snapshot; skipping it");

                continue;
            }

            WhiteboardSnapshot? snapshot = JsonSerializer.Deserialize<WhiteboardSnapshot>(data);

            if(snapshot is null || snapshot.SessionId != sessionId)
            {
                Trace.TraceWarning("[Whiteboard][WhiteboardSnapshotStore][ListAsync][Warning] " +
                "Snapshot data was invalid or belonged to another session; skipping it");
                continue;
            }

            snapshots.Add(new WhiteboardSnapshotSummary
            {
                SnapshotId = snapshot.SnapshotId,
                SavedBy = snapshot.SavedBy,
                SavedAt = snapshot.SavedAt
            });
        }

        List<WhiteboardSnapshotSummary> sortedSnapshots = snapshots.OrderByDescending(snapshot => snapshot.SavedAt).ToList();

        Trace.TraceInformation($"[Whiteboard][WhiteboardSnapshotStore][ListAsync][Information] " +
        $"Found {sortedSnapshots.Count} snapshots");

        return sortedSnapshots;
    }

    public async Task<WhiteboardSnapshot> LoadAsync(string sessionId, string snapshotId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);

        Trace.TraceInformation("[Whiteboard][WhiteboardSnapshotStore][LoadAsync][Information] Loading selected snapshot");

        var (success, data) = await _storage.Read(GetSnapshotKey(sessionId, snapshotId));

        if (!success)
        {
            Trace.TraceError("[Whiteboard][WhiteboardSnapshotStore][LoadAsync][Error] Cloud could not read the selected snapshot");

            throw new InvalidOperationException("Could not load the selected snapshot from Cloud");
        }

        WhiteboardSnapshot? snapshot = JsonSerializer.Deserialize<WhiteboardSnapshot>(data);

        if(snapshot is null || snapshot.SessionId != sessionId || snapshot.SnapshotId != snapshotId)
        {
            Trace.TraceError("[Whiteboard][WhiteboardSnapshotStore][LoadAsync][Error] " +
            "Loaded snapshot data was invalid.");
            
            throw new InvalidOperationException("The loaded snapshot is invalid");
        }

        Trace.TraceInformation($"[Whiteboard][WhiteboardSnapshotStore][LoadAsync][Information] " +
        $"Loaded snapshot with {snapshot.Items.Count} items.");

        return snapshot;
    }
}