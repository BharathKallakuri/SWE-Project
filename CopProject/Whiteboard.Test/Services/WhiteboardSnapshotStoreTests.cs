/// Author : Karthik Krishna M
/// Roll Number : 112301014
/// LICENSE : MIT

using Cloud.Persistence;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Services;

namespace Whiteboard.Test.Services;

public class WhiteboardSnapshotStoreTests
{
    [Fact]
    public async Task SaveSnapshotAsync_StoresSnapshot()
    {
        var storage = new InMemoryStorage();
        var store = new WhiteboardSnapshotStore(storage);
        var items = new List<ItemDto>();

        WhiteboardSnapshotSummary summary = await store.SaveSnapshotAsync("session-1", "user-1", items);

        WhiteboardSnapshot snapshot = await store.LoadAsync("session-1", summary.SnapshotId);

        Assert.Equal("session-1", snapshot.SessionId);
        Assert.Equal(summary.SnapshotId, snapshot.SnapshotId);
        Assert.Equal("user-1", snapshot.SavedBy);
        Assert.Empty(snapshot.Items);
        Assert.NotEqual(default, snapshot.SavedAt);
    }

    [Fact]
    public async Task ListAsync_ReturnSessionSnapshots()
    {
        var storage = new InMemoryStorage();
        var store = new WhiteboardSnapshotStore(storage);

        WhiteboardSnapshotSummary first = await store.SaveSnapshotAsync("session-1", "user-1", Array.Empty<ItemDto>());
        await Task.Delay(10);
        WhiteboardSnapshotSummary second = await store.SaveSnapshotAsync("session-1", "user-2", Array.Empty<ItemDto>());
        await store.SaveSnapshotAsync("session-2", "user-3", Array.Empty<ItemDto>());

        List<WhiteboardSnapshotSummary> snapshots = await store.ListAsync("session-1");

        Assert.Equal(2, snapshots.Count);
        Assert.Equal(second.SnapshotId, snapshots[0].SnapshotId);
        Assert.Equal(first.SnapshotId, snapshots[1].SnapshotId);
    }

    [Fact]
    public async Task SaveSnapshotAsync_StorageCreateFail()
    {
        var storage = new InMemoryStorage { FailCreate = true };
        var store = new WhiteboardSnapshotStore(storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveSnapshotAsync("session-1", "user-1", Array.Empty<ItemDto>()));
    }

    [Fact]
    public async Task LoadAsync_SnapshotDoesNotExist()
    {
        var store = new WhiteboardSnapshotStore(new InMemoryStorage());

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync("session-1", "missing-snapshot"));
    }

    [Fact]
    public async Task ListAsync_StorageCannotListKeys()
    {
        var storage = new InMemoryStorage { FailGetAllKeys = true };
        var store = new WhiteboardSnapshotStore(storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ListAsync("session-1"));
    }

    [Fact]
    public async Task ListAsync_SkipsUnreadableAndInvalidSnapshots()
    {
        var storage = new InMemoryStorage();
        var store = new WhiteboardSnapshotStore(storage);

        await storage.Create("snapshot:session-1:null-snap", System.Text.Encoding.UTF8.GetBytes("null"));
        await storage.Create("snapshot:session-1:wrong-session", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new WhiteboardSnapshot {
            SessionId = "other-session",
            SnapshotId = "wrong-session",
            SavedBy = "user-1"
        }));
        storage.FailReadKeys.Add("snapshot:session-1:unreadable");
        await storage.Create("snapshot:session-1:unreadable", System.Text.Encoding.UTF8.GetBytes("{}"));

        List<WhiteboardSnapshotSummary> list = await store.ListAsync("session-1");

        Assert.Empty(list);
    }

    [Fact]
    public async Task LoadAsync_ThrowsWhenSnapshotPayloadIsNullOrMismatched()
    {
        var storage = new InMemoryStorage();
        var store = new WhiteboardSnapshotStore(storage);

        await storage.Create("snapshot:session-1:null-snap", System.Text.Encoding.UTF8.GetBytes("null"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync("session-1", "null-snap"));

        await storage.Create("snapshot:session-1:bad-session", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new WhiteboardSnapshot {
            SessionId = "other",
            SnapshotId = "bad-session"
        }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync("session-1", "bad-session"));

        await storage.Create("snapshot:session-1:bad-id", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new WhiteboardSnapshot {
            SessionId = "session-1",
            SnapshotId = "different-id"
        }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync("session-1", "bad-id"));
    }

    [Fact]
    public void WhiteboardSnapshotSummary_ToString_FormatsDateAndUser()
    {
        DateTimeOffset savedAt = new(2026, 5, 1, 10, 30, 0, TimeSpan.Zero);
        var summary = new WhiteboardSnapshotSummary {
            SnapshotId = "s1",
            SavedBy = "alice",
            SavedAt = savedAt
        };

        Assert.Contains("alice", summary.ToString());
    }

    private sealed class InMemoryStorage : IStorage
    {
        private readonly Dictionary<string, byte[]> _items = new();

        public bool FailCreate { get; set; }
        public bool FailGetAllKeys { get; set; }
        public HashSet<string> FailReadKeys { get; } = new();

        public Task<bool> Create(string key, byte[] data)
        {
            if (FailCreate || _items.ContainsKey(key))
            {
                return Task.FromResult(false);
            }

            _items[key] = data;
            return Task.FromResult(true);
        }

        public Task<(bool Success, byte[] Data)> Read(string key)
        {
            if (FailReadKeys.Contains(key))
            {
                return Task.FromResult((false, Array.Empty<byte>()));
            }

            return Task.FromResult(_items.TryGetValue(key, out byte[]? data)
                ? (true, data!)
                : (false, Array.Empty<byte>()));
        }

        public Task<(bool Success, string Etag)> ReadEtag(string key)
        {
            return Task.FromResult((false, string.Empty));
        }

        public Task<bool> Update(string key, byte[] data, string etag)
        {
            return Task.FromResult(false);
        }

        public Task<bool> Delete(string key)
        {
            return Task.FromResult(_items.Remove(key));
        }

        public Task<bool> CreateAll(Dictionary<string, byte[]> items)
        {
            return Task.FromResult(false);
        }

        public Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys)
        {
            Dictionary<string, byte[]> found = _items.Where(entry => keys.Contains(entry.Key)).ToDictionary(entry => entry.Key, entry => entry.Value);

            return Task.FromResult((true, found));
        }

        public Task<(bool Success, List<string>)> GetAllKeys()
        {
            return Task.FromResult(FailGetAllKeys
                ? (false, new List<string>())
                : (true, _items.Keys.ToList()));
        }

        public Task<(bool Success, Dictionary<string, string>)> ReadAllEtags()
        {
            return Task.FromResult((false, new Dictionary<string, string>()));
        }

        public Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys)
        {
            return Task.FromResult((false, new Dictionary<string, string>()));
        }

        public Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags)
        {
            return Task.FromResult(false);
        }

        public Task<bool> DeleteAll(string[] keys)
        {
            return Task.FromResult(false);
        }

        public Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int n)
        {
            return Task.FromResult((true, new Dictionary<string, byte[]>()));
        }
    }
}
