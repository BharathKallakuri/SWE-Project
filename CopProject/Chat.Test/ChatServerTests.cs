using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using Chat;
using Networking;

namespace Chat.Test;

public class ChatServerTests
{
    private static readonly IPEndPoint ClientAddress = new(IPAddress.Loopback, 23456);

    [Fact]
    public void ConstructorRejectsNullChatroom()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();

        Assert.Throws<ArgumentNullException>(() => new ChatServer(null!, communicator, controller));
    }

    [Fact]
    public void ConstructorRejectsNullCommunicator()
    {
        RecordingController controller = new();

        Assert.Throws<ArgumentNullException>(
            () => new ChatServer(new Chatroom("room", "host"), null!, controller));
    }

    [Fact]
    public void ConstructorRejectsNullController()
    {
        RecordingCommunicator communicator = new();

        Assert.Throws<ArgumentNullException>(
            () => new ChatServer(new Chatroom("room", "host"), communicator, null!));
    }

    [Fact]
    public void ConstructorSubscribesToCommunicator()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);

        Assert.Equal(ChatConstants.ModuleId, communicator.SubscribedModule);
        Assert.Same(server, communicator.Subscriber);
    }

    [Fact]
    public void DisposeUnsubscribesFromCommunicatorAndController()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        ChatServer server = NewServer(communicator, controller);
        server.Dispose();
        server.Dispose();

        Assert.Equal(1, communicator.RemoveSubscriberCount);
        Assert.Equal(ChatConstants.ModuleId, communicator.RemovedModule);
        controller.RaiseParticipantAdded("guest");
        Assert.DoesNotContain("guest", server.Chatroom.Participants);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RegisterClientRejectsNullOrWhitespaceUsername(string? username)
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.ThrowsAny<ArgumentException>(() => server.RegisterClient(username!));
    }

    [Fact]
    public void RegisterClientAddsParticipantAndSendsInitialSyncAndParticipantSnapshot()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);

        bool registered = server.RegisterClient("guest");

        Assert.True(registered);
        Assert.Contains("guest", server.Chatroom.Participants);
        Assert.Equal(2, communicator.Sent.Count);
        Assert.IsType<ChatServerOperationBatch>(ReadUpdate(communicator.Sent[0]).Update);
        Assert.Equal(new[] { "guest", "host" }, ReadUpdate(communicator.Sent[1]).Participants);
    }

    [Fact]
    public void RegisterClientDoesNotRegisterSameUsernameTwice()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        server.RegisterClient("guest");
        int sentCount = communicator.Sent.Count;

        Assert.False(server.RegisterClient("guest"));
        Assert.Equal(sentCount, communicator.Sent.Count);
    }

    [Fact]
    public void UnregisterClientRemovesParticipantAndBroadcastsUpdatedList()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        controller.AddEndpoint("host", ClientAddress);
        server.RegisterClient("host");
        server.RegisterClient("guest");
        communicator.Sent.Clear();

        Assert.True(server.UnregisterClient("guest"));

        Assert.DoesNotContain("guest", server.Chatroom.Participants);
        Assert.Single(communicator.Sent);
        Assert.Equal(new[] { "host" }, ReadUpdate(communicator.Sent[0]).Participants);
    }

    [Fact]
    public void UnregisterClientKeepsHostInRoom()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("host", ClientAddress);
        server.RegisterClient("host");

        Assert.True(server.UnregisterClient("host"));
        Assert.Contains("host", server.Chatroom.Participants);
    }

    [Fact]
    public void UnregisterClientReturnsFalseForUnknownUsername()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.False(server.UnregisterClient("unknown"));
    }

    [Fact]
    public void ParticipantAddedEventRegistersClient()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);

        controller.RaiseParticipantAdded("guest");
        Assert.Contains("guest", server.Chatroom.Participants);
    }

    [Fact]
    public void ParticipantRemovedEventUnregistersClient()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        controller.RaiseParticipantAdded("guest");
        controller.RaiseParticipantRemoved("guest");

        Assert.DoesNotContain("guest", server.Chatroom.Participants);
    }

    [Fact]
    public void HandleRequestRejectsNullEnvelope()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.Throws<ArgumentNullException>(() => server.HandleRequest(null!));
    }

    [Fact]
    public void HandleRequestRejectsNullRequest()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.Throws<ArgumentNullException>(
            () => server.HandleRequest(new ChatClientRequestEnvelope(Guid.NewGuid(), null!, "guest")));
    }

    [Fact]
    public void HandleRequestRejectsWhitespaceUsername()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.ThrowsAny<ArgumentException>(
            () => server.HandleRequest(new ChatClientRequestEnvelope(Guid.NewGuid(), new Sync(), " ")));
    }

    [Fact]
    public void HandleRequestForUnknownClientReturnsForbidden()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        Guid requestId = Guid.NewGuid();
        List<string> failures = new();
        server.DeliveryFailed += failures.Add;

        server.HandleRequest(new ChatClientRequestEnvelope(requestId, new Sync(), "guest"));

        Assert.Empty(communicator.Sent);
        Assert.Equal(new[] { "guest" }, failures);
    }

    [Fact]
    public void SyncSendsOperationsAfterClientsCurrentVersion()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        server.RegisterClient("guest");
        server.SubmitHostRequest(new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "First", null));
        communicator.Sent.Clear();

        server.HandleRequest(new ChatClientRequestEnvelope(Guid.NewGuid(), new Sync(), "guest"));

        ChatServerOperationBatch batch = Assert.IsType<ChatServerOperationBatch>(
            ReadUpdate(Assert.Single(communicator.Sent)).Update);
        Assert.Equal(1, batch.FromVersion);
        Assert.Equal(1, batch.ToVersion);
        Assert.Empty(batch.Operations);
    }

    [Fact]
    public void ResetSendsResetThenFullOperationBatch()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        server.RegisterClient("guest");
        server.SubmitHostRequest(new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "Hello", null));
        communicator.Sent.Clear();

        server.HandleRequest(new ChatClientRequestEnvelope(Guid.NewGuid(), new Reset(), "guest"));

        Assert.Equal(2, communicator.Sent.Count);
        Assert.IsType<ChatServerReset>(ReadUpdate(communicator.Sent[0]).Update);
        ChatServerOperationBatch batch = Assert.IsType<ChatServerOperationBatch>(
            ReadUpdate(communicator.Sent[1]).Update);
        Assert.Equal(0, batch.FromVersion);
        Assert.Equal(1, batch.ToVersion);
        Assert.Single(batch.Operations);
    }

    [Fact]
    public void ClientMessageRequestUpdatesRoomRaisesOperationAndSendsResultAndBatch()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        server.RegisterClient("guest");
        communicator.Sent.Clear();
        Guid requestId = Guid.NewGuid();
        List<ChatOperation> applied = new();
        server.OperationApplied += applied.Add;

        server.HandleRequest(new ChatClientRequestEnvelope(
            requestId,
            new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "Hello", null),
            "guest"));

        Assert.Equal(1, server.Version);
        Assert.True(server.Chatroom.TryGetMessage(
            Assert.IsType<MessageAdded>(Assert.Single(applied)).MessageId,
            out ChatMessage message));
        Assert.Equal("guest", message.SenderName);
        Assert.Equal("Hello", message.Content);
        Assert.Equal(2, communicator.Sent.Count);
        ChatServerRequestResult result = ReadRequestResult(communicator.Sent[0]);
        Assert.Equal(requestId, result.RequestId);
        Assert.Equal(RoomResult.Ok, result.Result);
        ChatServerOperationBatch batch = Assert.IsType<ChatServerOperationBatch>(
            ReadUpdate(communicator.Sent[1]).Update);
        Assert.Single(batch.Operations);
    }

    [Fact]
    public void ClientThreadReplyRequestAddsReplyAndRecordsOperation()
    {
        (ChatServer server, RecordingCommunicator communicator, Guid parentMessageId) =
            NewServerWithGuestMessage();
        using (server)
        {
            server.HandleRequest(new ChatClientRequestEnvelope(
                Guid.NewGuid(),
                new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "Thread reply", parentMessageId),
                "guest"));

            Assert.Equal(1, server.Version);
            Assert.True(server.Chatroom.TryGetMessage(parentMessageId, out ChatMessage parent));
            ChatMessage reply = Assert.Single(Assert.IsType<ChatMessageWithThreads>(parent).ThreadById.Values);
            Assert.Equal("Thread reply", reply.Content);
            Assert.Equal(parentMessageId, reply.ParentMessageId);
            MessageAdded operation = Assert.IsType<MessageAdded>(
                Assert.Single(Assert.IsType<ChatServerOperationBatch>(
                    ReadUpdate(communicator.Sent[1]).Update).Operations));
            Assert.Equal(parentMessageId, operation.Message.ParentMessageId);
            Assert.Equal(reply.Content, operation.Message.Content);
        }
    }

    [Fact]
    public void ClientEditRequestUpdatesMessageAndRecordsEdit()
    {
        (ChatServer server, RecordingCommunicator communicator, Guid messageId) = NewServerWithGuestMessage();
        using (server)
        {
            server.HandleRequest(new ChatClientRequestEnvelope(
                Guid.NewGuid(),
                new EditMessage(messageId, "Edited", null),
                "guest"));

            Assert.Equal(1, server.Version);
            Assert.True(server.Chatroom.TryGetMessage(messageId, out ChatMessage message));
            Assert.Equal("Edited", message.Content);
            Assert.IsType<MessageEdited>(
                Assert.Single(Assert.IsType<ChatServerOperationBatch>(
                    ReadUpdate(communicator.Sent[1]).Update).Operations));
        }
    }

    [Fact]
    public void ClientDeleteRequestDeletesMessageAndRecordsDeletion()
    {
        (ChatServer server, RecordingCommunicator communicator, Guid messageId) = NewServerWithGuestMessage();
        using (server)
        {
            server.HandleRequest(new ChatClientRequestEnvelope(
                Guid.NewGuid(),
                new DeleteMessage(messageId, null),
                "guest"));

            Assert.Equal(1, server.Version);
            Assert.True(server.Chatroom.TryGetMessage(messageId, out ChatMessage message));
            Assert.True(message.IsDeleted);
            Assert.IsType<MessageDeleted>(
                Assert.Single(Assert.IsType<ChatServerOperationBatch>(
                    ReadUpdate(communicator.Sent[1]).Update).Operations));
        }
    }

    [Fact]
    public void ClientReactionRequestTogglesReactionAndRecordsChange()
    {
        (ChatServer server, RecordingCommunicator communicator, Guid messageId) = NewServerWithGuestMessage();
        using (server)
        {
            server.HandleRequest(new ChatClientRequestEnvelope(
                Guid.NewGuid(),
                new ToggleReaction(messageId, "👍", null),
                "guest"));

            Assert.Equal(1, server.Version);
            Assert.True(server.Chatroom.TryGetMessage(messageId, out ChatMessage message));
            Assert.Contains("guest", message.Reactions["👍"]);
            Assert.IsType<ReactionToggled>(
                Assert.Single(Assert.IsType<ChatServerOperationBatch>(
                    ReadUpdate(communicator.Sent[1]).Update).Operations));
        }
    }

    [Fact]
    public void RejectedClientRequestSendsResultWithoutRecordingOperation()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        server.RegisterClient("guest");
        communicator.Sent.Clear();
        Guid requestId = Guid.NewGuid();

        server.HandleRequest(new ChatClientRequestEnvelope(
            requestId,
            new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "Reply", Guid.NewGuid()),
            "guest"));

        Assert.Equal(0, server.Version);
        ChatServerRequestResult result = ReadRequestResult(Assert.Single(communicator.Sent));
        Assert.Equal(requestId, result.RequestId);
        Assert.Equal(RoomResult.NotFound, result.Result);
    }

    [Fact]
    public void SubmitHostRequestAppliesChangeAndSynchronizesRegisteredClient()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        server.RegisterClient("guest");
        communicator.Sent.Clear();
        List<ChatOperation> applied = new();
        server.OperationApplied += applied.Add;

        RoomResult result = server.SubmitHostRequest(
            new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "From host", null));

        Assert.Equal(RoomResult.Ok, result);
        Assert.Equal(1, server.Version);
        MessageAdded operation = Assert.IsType<MessageAdded>(Assert.Single(applied));
        Assert.Equal("host", operation.Message.SenderName);
        Assert.Single(communicator.Sent);
        Assert.IsType<ChatServerOperationBatch>(ReadUpdate(communicator.Sent[0]).Update);
    }

    [Fact]
    public void SubmitHostRequestReturnsRejectedResultWithoutRecordingOperation()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        RoomResult result = server.SubmitHostRequest(
            new EditMessage(Guid.NewGuid(), "Edited", null));

        Assert.Equal(RoomResult.NotFound, result);
        Assert.Equal(0, server.Version);
    }

    [Fact]
    public void DeliveryFailureDoesNotAdvanceClientVersionAndRaisesEvent()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        List<string> failures = new();
        server.DeliveryFailed += failures.Add;

        Assert.True(server.RegisterClient("guest"));
        Assert.Equal(new[] { "guest", "guest" }, failures);
        Assert.Empty(communicator.Sent);

        controller.AddEndpoint("guest", ClientAddress);
        communicator.Sent.Clear();
        server.HandleRequest(new ChatClientRequestEnvelope(Guid.NewGuid(), new Sync(), "guest"));

        ChatServerOperationBatch batch = Assert.IsType<ChatServerOperationBatch>(
            ReadUpdate(Assert.Single(communicator.Sent)).Update);
        Assert.Equal(0, batch.FromVersion);
    }

    [Fact]
    public void OnMessageReceivedDeserializesAndHandlesRequest()
    {
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        using ChatServer server = NewServer(communicator, controller);
        controller.AddEndpoint("guest", ClientAddress);
        server.RegisterClient("guest");
        communicator.Sent.Clear();
        ChatClientRequestEnvelope request = new(
            Guid.NewGuid(),
            new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "Via listener", null),
            "guest");

        server.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes(request));

        Assert.Equal(1, server.Version);
        Assert.Equal("Via listener", Assert.IsType<MessageAdded>(
            Assert.Single(Assert.IsType<ChatServerOperationBatch>(
                ReadUpdate(communicator.Sent[1]).Update).Operations)).Message.Content);
    }

    [Fact]
    public void OnMessageReceivedRejectsNullPayload()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.Throws<ArgumentNullException>(() => server.OnMessageReceived(null!));
    }

    [Fact]
    public void OnMessageReceivedRejectsEmptyPayload()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.Throws<InvalidDataException>(
            () => server.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes<ChatClientRequestEnvelope?>(null)));
    }

    [Fact]
    public void OnMessageReceivedRejectsMalformedJson()
    {
        using ChatServer server = NewServer(new RecordingCommunicator(), new RecordingController());

        Assert.Throws<JsonException>(() => server.OnMessageReceived(new byte[] { (byte)'{' }));
    }

    private static ChatServer NewServer(RecordingCommunicator communicator, RecordingController controller)
    {
        return new ChatServer(new Chatroom("room", "host"), communicator, controller);
    }

    private static (ChatServer Server, RecordingCommunicator Communicator, Guid MessageId)
        NewServerWithGuestMessage()
    {
        Chatroom chatroom = new("room", "host");
        Guid messageId = chatroom.AddMessage(new ChatMessageWithThreads
        {
            Timestamp = DateTimeOffset.UtcNow,
            SenderName = "guest",
            Content = "Original"
        });
        RecordingCommunicator communicator = new();
        RecordingController controller = new();
        controller.AddEndpoint("guest", ClientAddress);
        ChatServer server = new(chatroom, communicator, controller);
        server.RegisterClient("guest");
        communicator.Sent.Clear();
        return (server, communicator, messageId);
    }

    private static ChatServerUpdateEnvelope ReadUpdate(SentMessage sent)
    {
        ChatServerMessage message = JsonSerializer.Deserialize<ChatServerMessage>(sent.Payload)!;
        return Assert.IsType<ChatServerUpdateEnvelope>(message);
    }

    private static ChatServerRequestResult ReadRequestResult(SentMessage sent)
    {
        ChatServerMessage message = JsonSerializer.Deserialize<ChatServerMessage>(sent.Payload)!;
        return Assert.IsType<ChatServerRequestResult>(message);
    }

    private sealed record SentMessage(IPEndPoint Destination, byte[] Payload, int Module, int Priority);

    private sealed class RecordingCommunicator : ICommunicator
    {
        public int ListenPort => 0;
        public IPAddress ListenIpAddress => IPAddress.Loopback;
        public int? SubscribedModule { get; private set; }
        public IMessageListener? Subscriber { get; private set; }
        public int RemoveSubscriberCount { get; private set; }
        public int? RemovedModule { get; private set; }
        public List<SentMessage> Sent { get; } = new();

        public void AddSubscriber(int module, IMessageListener subscriber)
        {
            SubscribedModule = module;
            Subscriber = subscriber;
        }

        public void RemoveSubscriber(int module)
        {
            RemoveSubscriberCount++;
            RemovedModule = module;
        }

        public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
        {
            Sent.Add(new SentMessage(iPEndPoint, message, module, priority));
        }

        public void BroadcastMessage(byte[] message, int module, int priority)
        {
        }

        public void JoinMulticastGroup(int multicastGroup)
        {
        }

        public void LeaveMulticastGroup(int multicastGroup)
        {
        }

        public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
        {
        }
    }

    private sealed class RecordingController : IChatController
    {
        private readonly Dictionary<string, IPEndPoint> _endpoints = new(StringComparer.Ordinal);

        public event Action<string>? ParticipantAdded;
        public event Action<string>? ParticipantRemoved;

        public void AddEndpoint(string username, IPEndPoint endpoint)
        {
            _endpoints[username] = endpoint;
        }

        public void RaiseParticipantAdded(string username)
        {
            ParticipantAdded?.Invoke(username);
        }

        public void RaiseParticipantRemoved(string username)
        {
            ParticipantRemoved?.Invoke(username);
        }

        public IPEndPoint GetParticipantEndPoint(string username)
        {
            return _endpoints.TryGetValue(username, out IPEndPoint? endpoint)
                ? endpoint
                : throw new KeyNotFoundException($"No endpoint for {username}.");
        }
    }
}
