using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using FileSynchroniser.Models;
using FileSynchroniser.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Networking;

namespace Filesync.Test;

[TestClass]
public class NetworkSyncHandlerTest
{
    private string _tempDir = null!;
    private FakeCommunicator _communicator = null!;
    private NetworkSyncHandler _handler = null!;

    /// <summary>
    /// Simple fake communicator for testing NetworkSyncHandler without opening real sockets
    /// </summary>
    private class FakeCommunicator : ICommunicator
    {
        public int ListenPort => 8001;
        public IPAddress ListenIpAddress => IPAddress.Loopback;
        public IMessageListener? Subscriber { get; private set; }
        public int SubscribedModule { get; private set; }
        public List<(IPEndPoint? endpoint, byte[] data, int module, int priority)> SentMessages { get; } = new();
        public List<(byte[] data, int module, int priority)> BroadcastMessages { get; } = new();

        public void AddSubscriber(int module, IMessageListener subscriber)
        {
            SubscribedModule = module;
            Subscriber = subscriber;
        }

        public void RemoveSubscriber(int module)
        {
            if (SubscribedModule == module)
            {
                Subscriber = null;
            }
        }

        public void SendMessage(IPEndPoint ipEndPoint, byte[] message, int module, int priority)
        {
            SentMessages.Add((ipEndPoint, message, module, priority));
        }

        public void BroadcastMessage(byte[] message, int module, int priority)
        {
            BroadcastMessages.Add((message, module, priority));
        }

        public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority) { }
        public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp) { }
        public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp) { }
    }

    /// <summary>
    /// Initializing test temp folder and handler before each test
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SyncHandlerTests_{Guid.NewGuid():N}");
        _communicator = new FakeCommunicator();
        _handler = new NetworkSyncHandler(_tempDir, _communicator);
    }

    /// <summary>
    /// Clean up temp directory once test is completed
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    /// <summary>
    /// 1. Constructor creates root folder and subscribes to module 5
    /// </summary>
    [TestMethod]
    public void Constructor_CreatesDirectoryAndRegistersSubscriber()
    {
        Assert.IsTrue(Directory.Exists(_tempDir));
        Assert.AreEqual(Path.GetFullPath(_tempDir), _handler.RootDirectory);
        Assert.AreEqual(NetworkSyncHandler.ModuleId, _communicator.SubscribedModule);
        Assert.AreSame(_handler, _communicator.Subscriber);
    }

    /// <summary>
    /// 2. StartSync sends GET_MANIFEST to DefaultPeerEndpoint when specified
    /// </summary>
    [TestMethod]
    public void StartSync_WithDefaultPeer_SendsGetManifestToPeer()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        _handler.StartSync();

        Assert.HasCount(1, _communicator.SentMessages);
        Assert.AreEqual(endpoint, _communicator.SentMessages[0].endpoint);
        string msg = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.AreEqual("GET_MANIFEST", msg);
    }

    /// <summary>
    /// 3. StartSync broadcasts GET_MANIFEST when no default peer is set
    /// </summary>
    [TestMethod]
    public void StartSync_WithoutDefaultPeer_BroadcastsGetManifest()
    {
        _handler.DefaultPeerEndpoint = null;

        _handler.StartSync();

        Assert.HasCount(1, _communicator.BroadcastMessages);
        string msg = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.AreEqual("GET_MANIFEST", msg);
    }

    /// <summary>
    /// 4. Receiving GET_MANIFEST sends local files manifest
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_SendsLocalManifest()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        // create a test file locally
        string testFile = Path.Combine(_tempDir, "sample.txt");
        File.WriteAllText(testFile, "hello filesync");

        byte[] req = Encoding.UTF8.GetBytes("GET_MANIFEST");
        _handler.OnMessageReceived(req);

        Assert.HasCount(1, _communicator.SentMessages);
        string response = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.StartsWith("MANIFEST:", response);
        Assert.Contains("sample.txt", response);
    }

    /// <summary>
    /// 5. Receiving GET_MANIFEST without default peer broadcasts local manifest
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_GetManifest_WithoutPeer_BroadcastsLocalManifest()
    {
        _handler.DefaultPeerEndpoint = null;

        byte[] req = Encoding.UTF8.GetBytes("GET_MANIFEST");
        _handler.OnMessageReceived(req);

        Assert.HasCount(1, _communicator.BroadcastMessages);
        string response = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.StartsWith("MANIFEST:", response);
    }

    /// <summary>
    /// 6. HandleManifest requests files that are missing locally
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_RequestsMissingFile()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        var remoteManifest = new Dictionary<string, DateTime>
        {
            { "missing.txt", DateTime.UtcNow }
        };
        string json = JsonSerializer.Serialize(remoteManifest);
        byte[] payload = Encoding.UTF8.GetBytes("MANIFEST:" + json);

        _handler.OnMessageReceived(payload);

        Assert.HasCount(1, _communicator.SentMessages);
        string sent = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.AreEqual("REQUEST:missing.txt", sent);
    }

    /// <summary>
    /// 7. HandleManifest requests files when remote version is newer
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_RequestsNewerFile()
    {
        _handler.DefaultPeerEndpoint = null;

        string localFile = Path.Combine(_tempDir, "older.txt");
        File.WriteAllText(localFile, "old content");
        File.SetLastWriteTimeUtc(localFile, DateTime.UtcNow.AddHours(-2));

        var remoteManifest = new Dictionary<string, DateTime>
        {
            { "older.txt", DateTime.UtcNow }
        };
        string json = JsonSerializer.Serialize(remoteManifest);
        byte[] payload = Encoding.UTF8.GetBytes("MANIFEST:" + json);

        _handler.OnMessageReceived(payload);

        Assert.HasCount(1, _communicator.BroadcastMessages);
        string sent = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.AreEqual("REQUEST:older.txt", sent);
    }

    /// <summary>
    /// 8. HandleManifest triggers OnSyncCompleted when all files are already up-to-date
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_WhenUpToDate_InvokesSyncCompleted()
    {
        string localFile = Path.Combine(_tempDir, "current.txt");
        File.WriteAllText(localFile, "current content");
        DateTime fileTime = File.GetLastWriteTimeUtc(localFile);

        var remoteManifest = new Dictionary<string, DateTime>
        {
            { "current.txt", fileTime.AddHours(-1) }
        };
        string json = JsonSerializer.Serialize(remoteManifest);
        byte[] payload = Encoding.UTF8.GetBytes("MANIFEST:" + json);

        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        _handler.OnMessageReceived(payload);

        Assert.IsTrue(completed);
        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 9. HandleManifest with invalid JSON does not throw or crash
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Manifest_InvalidJson_DoesNotThrow()
    {
        byte[] payload = Encoding.UTF8.GetBytes("MANIFEST:invalid json data");
        _handler.OnMessageReceived(payload);

        Assert.HasCount(0, _communicator.SentMessages);
    }

    /// <summary>
    /// 10. HandleFileRequest splits local file and sends chunks to peer
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_ValidFile_SendsChunksToPeer()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 8002);
        _handler.DefaultPeerEndpoint = endpoint;

        string localFile = Path.Combine(_tempDir, "data.txt");
        File.WriteAllText(localFile, "chunk file test data");

        byte[] req = Encoding.UTF8.GetBytes("REQUEST:data.txt");
        _handler.OnMessageReceived(req);

        Assert.IsGreaterThanOrEqualTo(_communicator.SentMessages.Count, 1);
        string firstChunkPacket = Encoding.UTF8.GetString(_communicator.SentMessages[0].data);
        Assert.StartsWith("CHUNK:", firstChunkPacket);
    }

    /// <summary>
    /// 11. HandleFileRequest broadcasts chunks when DefaultPeerEndpoint is null
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_WithoutDefaultPeer_BroadcastsChunks()
    {
        _handler.DefaultPeerEndpoint = null;

        string localFile = Path.Combine(_tempDir, "broadcast.txt");
        File.WriteAllText(localFile, "broadcast chunk data");

        byte[] req = Encoding.UTF8.GetBytes("REQUEST:broadcast.txt");
        _handler.OnMessageReceived(req);

        Assert.IsGreaterThanOrEqualTo(_communicator.BroadcastMessages.Count, 1);
        string packet = Encoding.UTF8.GetString(_communicator.BroadcastMessages[0].data);
        Assert.StartsWith("CHUNK:", packet);
    }

    /// <summary>
    /// 12. HandleFileRequest rejects non-existent files or path traversal attempts
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Request_InvalidOrOutsideRoot_RejectsRequest()
    {
        // non-existent file
        byte[] req1 = Encoding.UTF8.GetBytes("REQUEST:ghost.txt");
        _handler.OnMessageReceived(req1);

        // path traversal attempt
        byte[] req2 = Encoding.UTF8.GetBytes("REQUEST:../../etc/passwd");
        _handler.OnMessageReceived(req2);

        Assert.HasCount(0, _communicator.SentMessages);
        Assert.HasCount(0, _communicator.BroadcastMessages);
    }

    /// <summary>
    /// 13. HandleChunk reassembles incoming chunks into file and raises OnSyncCompleted
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_ReassemblesFileAndInvokesSyncCompleted()
    {
        bool completed = false;
        _handler.OnSyncCompleted += () => completed = true;

        byte[] fileBytes = Encoding.UTF8.GetBytes("full reconstructed file content");
        string base64 = Convert.ToBase64String(fileBytes);

        var chunkDto = new
        {
            FileId = "received.txt",
            ChunkNumber = 0,
            TotalChunks = 1,
            Base64Data = base64
        };

        string json = JsonSerializer.Serialize(chunkDto);
        byte[] packet = Encoding.UTF8.GetBytes("CHUNK:" + json);

        _handler.OnMessageReceived(packet);

        Assert.IsTrue(completed);
        string assembledPath = Path.Combine(_tempDir, "received.txt");
        Assert.IsTrue(File.Exists(assembledPath));
        Assert.AreEqual("full reconstructed file content", File.ReadAllText(assembledPath));
    }

    /// <summary>
    /// 14. HandleChunk with invalid JSON does not throw
    /// </summary>
    [TestMethod]
    public void OnMessageReceived_Chunk_InvalidJson_DoesNotThrow()
    {
        byte[] packet = Encoding.UTF8.GetBytes("CHUNK:not a json");
        _handler.OnMessageReceived(packet);

        Assert.IsFalse(File.Exists(Path.Combine(_tempDir, "invalid.txt")));
    }

    /// <summary>
    /// 15. OnLog fires event when log messages are emitted
    /// </summary>
    [TestMethod]
    public void OnLog_FiresLogEvent()
    {
        string? loggedMessage = null;
        _handler.OnLog += msg => loggedMessage = msg;

        _handler.StartSync();

        Assert.IsNotNull(loggedMessage);
        Assert.Contains("[sync]", loggedMessage);
    }
}
