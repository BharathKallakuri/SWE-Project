// Author: Rajat
// Roll no.: 112301044

using System.Net;
using System.Text.Json;
using Chat;
using Controller;
using UI.Services;
using Utils;

namespace Chat.UI.Test;

public class ChatClientAdapterTests
{
    private static ChatClientAdapter ClientAdapter(
        RecordingCommunicator? communicator,
        RecordingController? controller,
        out ChatClient client)
    {
        Chatroom room = new("meeting", "hostuser");
        communicator ??= new RecordingCommunicator();
        ChatClientAdapter adapter = new(
            client = new ChatClient(
                "alice",
                room,
                communicator,
                new IPEndPoint(IPAddress.Loopback, 12345)),
            DispatcherThread.Queue,
            controller);
        return adapter;
    }

    private static ChatClientAdapter HostAdapter(
        RecordingController controller,
        out ChatServer server,
        out Chatroom room)
    {
        room = new("meeting", "hostuser");
        CommunicatorSetup(controller, out RecordingCommunicator communicator);
        server = new(
            room,
            controller,
            () => Task.FromResult(new Insights.AI.InsightsManager(new InsightsTestProvider("AI answer."))));
        return new ChatClientAdapter(server, controller, DispatcherThread.Queue);
    }

    private static void CommunicatorSetup(RecordingController controller, out RecordingCommunicator communicator)
    {
        communicator = new RecordingCommunicator();
        controller.Communicator = communicator;
    }

    // ---- constructors / identity ------------------------------------------

    [Fact]
    public void ClientConstructor_NullClientOrDispatcher_Throws()
    {
        ChatClient client = new(
            "alice",
            new Chatroom("meeting", "hostuser"),
            new RecordingCommunicator(),
            new IPEndPoint(IPAddress.Loopback, 12345));

        Assert.Throws<ArgumentNullException>(() => new ChatClientAdapter(null!, DispatcherThread.Queue));
        Assert.Throws<ArgumentNullException>(() => new ChatClientAdapter(client, null!));
    }

    [Fact]
    public void HostConstructor_NullServerControllerOrDispatcher_Throws()
    {
        RecordingController controller = new("hostuser") {
            Communicator = new RecordingCommunicator()
        };
        ChatServer server = new(
            new Chatroom("meeting", "hostuser"),
            controller,
            () => Task.FromResult(new Insights.AI.InsightsManager(new InsightsTestProvider("AI answer."))));

        Assert.Throws<ArgumentNullException>(() => new ChatClientAdapter(null!, controller, DispatcherThread.Queue));
        Assert.Throws<ArgumentNullException>(() => new ChatClientAdapter(server, null!, DispatcherThread.Queue));
        Assert.Throws<ArgumentNullException>(() => new ChatClientAdapter(server, controller, null!));
    }

    [Fact]
    public void ClientAdapter_ReportsClientIdentity_AndIsNotHost()
    {
        DispatcherThread.Run(() => {
            ChatClientAdapter adapter = ClientAdapter(null, null, out ChatClient client);
            using (adapter)
            {
                Assert.Equal("alice", adapter.Username);
                Assert.Equal("hostuser", adapter.HostName);
                Assert.False(adapter.IsHost);
                Assert.Contains("hostuser", adapter.Participants);
            }

            _ = client;
        });
    }

    [Fact]
    public void HostAdapter_ReportsHostIdentity_AndIsHost()
    {
        DispatcherThread.Run(() => {
            RecordingController controller = new("hostuser");
            ChatClientAdapter adapter = HostAdapter(controller, out ChatServer server, out _);
            using (adapter)
            {
                Assert.Equal("hostuser", adapter.Username);
                Assert.Equal("hostuser", adapter.HostName);
                Assert.True(adapter.IsHost);
                Assert.Contains("hostuser", adapter.Participants);
            }

            _ = server;
        });
    }

    [Fact]
    public void GetMeetingUsers_ReturnsControllerUsers_WhenAttached()
    {
        DispatcherThread.Run(() => {
            RecordingController controller = new("alice");
            controller.AddUser("bob", TestImages.OnePixelPng);
            ChatClientAdapter adapter = ClientAdapter(null, controller, out _);
            using (adapter)
            {
                List<ControllerUser> users = adapter.GetMeetingUsers().ToList();
                Assert.Equal(2, users.Count);
                Assert.Contains(users, user => user.Username == "alice");
                Assert.Contains(users, user => user.Username == "bob");
            }
        });
    }

    [Fact]
    public void GetMeetingUsers_ReturnsEmptyList_WhenNoControllerAttached()
    {
        DispatcherThread.Run(() => {
            ChatClientAdapter adapter = ClientAdapter(null, null, out _); // no controller passed
            using (adapter)
            {
                Assert.Empty(adapter.GetMeetingUsers());
            }
        });
    }

    // ---- sync / reset -----------------------------------------------------

    [Fact]
    public void Sync_OnClient_SendsSyncRequestOverWire()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            using (adapter)
            {
                communicator.ClearSent();

                Guid id = adapter.Sync();

                ChatClientRequestBody request = Assert.Single(communicator.ReadRequests());
                Assert.Equal(id, request.RequestId);
                Assert.IsType<Sync>(request.Request);
            }
        });
    }

    [Fact]
    public void Sync_OnHost_SendsNothing_ButRaisesParticipantsChanged()
    {
        RecordingController controller = new("hostuser");
        List<IReadOnlySet<string>> raised = new();
        ChatClientAdapter adapter = null!;

        DispatcherThread.Run(() => {
            adapter = HostAdapter(controller, out _, out _);
            adapter.ParticipantsChanged += participants => raised.Add(participants);
            adapter.Sync();
        });

        DispatcherThread.Flush(); // the host raises participants on the dispatcher queue

        DispatcherThread.Run(() => {
            using (adapter)
            {
                Assert.Single(raised);
                Assert.Contains("hostuser", raised[0]);
            }
        });
    }

    [Fact]
    public void Reset_OnClient_SendsResetRequestOverWire()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            using (adapter)
            {
                communicator.ClearSent();

                Guid id = adapter.Reset();

                ChatClientRequestBody request = Assert.Single(communicator.ReadRequests());
                Assert.Equal(id, request.RequestId);
                Assert.IsType<Reset>(request.Request);
            }
        });
    }

    [Fact]
    public void Reset_OnHost_IsNoOp()
    {
        DispatcherThread.Run(() => {
            RecordingController controller = new("hostuser");
            ChatClientAdapter adapter = HostAdapter(controller, out _, out Chatroom room);
            using (adapter)
            {
                // Host reset does not go over the wire; the host is the source of truth.
                Guid id = adapter.Reset();

                Assert.NotEqual(Guid.Empty, id);
            }
        });
    }

    // ---- host operations --------------------------------------------------

    [Fact]
    public void SendMessage_OnClient_SendsRequestOverWire()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            using (adapter)
            {
                communicator.ClearSent();

                Guid id = adapter.SendMessage("hello", new Mentions(), threadParentMessageId: null, replyToMessageId: null);

                ChatClientRequestBody request = Assert.Single(communicator.ReadRequests());
                Assert.Equal(id, request.RequestId);
                SendMessage sent = Assert.IsType<SendMessage>(request.Request);
                Assert.Equal("hello", sent.Content);
            }
        });
    }

    [Fact]
    public void SendMessage_OnHost_AppliesChangeToRoom_AndCompletesRequest()
    {
        RecordingController controller = new("hostuser");
        List<(Guid Id, RoomResult Result)> completions = new();
        ChatClientAdapter adapter = null!;
        Chatroom room = null!;
        Guid requestId = Guid.Empty;

        DispatcherThread.Run(() => {
            adapter = HostAdapter(controller, out _, out room);
            adapter.RequestCompleted += (id, result) => completions.Add((id, result));
            requestId = adapter.SendMessage("hello from host", new Mentions());
        });

        DispatcherThread.Flush(); // host completion is enqueued

        DispatcherThread.Run(() => {
            using (adapter)
            {
                Assert.Contains(room.GetMessagesSnapshot(), message => message.Message.Content == "hello from host");
                (Guid Id, RoomResult Result) completion = Assert.Single(completions);
                Assert.Equal(requestId, completion.Id);
                Assert.Equal(RoomResult.Ok, completion.Result);
            }
        });
    }

    [Fact]
    public void EditMessage_OnHost_AppliesChangeToRoom()
    {
        RecordingController controller = new("hostuser");
        ChatClientAdapter adapter = null!;
        Chatroom room = null!;

        DispatcherThread.Run(() => {
            adapter = HostAdapter(controller, out _, out room);
            adapter.SendMessage("before edit", new Mentions());
        });

        DispatcherThread.Flush();

        DispatcherThread.Run(() => {
            Guid roomMessageId = room.GetMessagesSnapshot().Single().Id;
            adapter.EditMessage(roomMessageId, "after edit");
        });

        DispatcherThread.Flush();

        DispatcherThread.Run(() => {
            using (adapter)
            {
                Assert.Equal("after edit", room.GetMessagesSnapshot().Single().Message.Content);
            }
        });
    }

    [Fact]
    public void DeleteMessage_OnHost_AppliesChangeToRoom()
    {
        RecordingController controller = new("hostuser");
        ChatClientAdapter adapter = null!;
        Chatroom room = null!;

        DispatcherThread.Run(() => {
            adapter = HostAdapter(controller, out _, out room);
            adapter.SendMessage("delete me", new Mentions());
        });

        DispatcherThread.Flush();

        DispatcherThread.Run(() => {
            Guid roomMessageId = room.GetMessagesSnapshot().Single().Id;
            adapter.DeleteMessage(roomMessageId);
        });

        DispatcherThread.Flush();

        DispatcherThread.Run(() => {
            using (adapter)
            {
                (Guid Id, ChatMessage Message, Guid? ThreadParentMessageId) only =
                    Assert.Single(room.GetMessagesSnapshot());
                Assert.True(only.Message.IsDeleted);
            }
        });
    }

    [Fact]
    public void ToggleReaction_OnHost_AppliesChangeToRoom()
    {
        RecordingController controller = new("hostuser");
        ChatClientAdapter adapter = null!;
        Chatroom room = null!;

        DispatcherThread.Run(() => {
            adapter = HostAdapter(controller, out _, out room);
            adapter.SendMessage("react me", new Mentions());
        });

        DispatcherThread.Flush();

        DispatcherThread.Run(() => {
            Guid roomMessageId = room.GetMessagesSnapshot().Single().Id;
            adapter.ToggleReaction(roomMessageId, "👍");
        });

        DispatcherThread.Flush();

        DispatcherThread.Run(() => {
            using (adapter)
            {
                Assert.Contains("hostuser", room.GetMessagesSnapshot().Single().Message.Reactions["👍"]);
            }
        });
    }

    [Fact]
    public void EditMessage_OnClient_SendsRequestOverWire()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            Guid messageId = Guid.NewGuid();
            using (adapter)
            {
                communicator.ClearSent();

                Guid id = adapter.EditMessage(messageId, "new content");

                ChatClientRequestBody request = Assert.Single(communicator.ReadRequests());
                Assert.Equal(id, request.RequestId);
                EditMessage sent = Assert.IsType<EditMessage>(request.Request);
                Assert.Equal(messageId, sent.MessageId);
                Assert.Equal("new content", sent.NewContent);
            }
        });
    }

    [Fact]
    public void DeleteMessage_OnClient_SendsRequestOverWire()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            Guid messageId = Guid.NewGuid();
            using (adapter)
            {
                communicator.ClearSent();

                Guid id = adapter.DeleteMessage(messageId, threadParentMessageId: Guid.NewGuid());

                ChatClientRequestBody request = Assert.Single(communicator.ReadRequests());
                Assert.Equal(id, request.RequestId);
                DeleteMessage sent = Assert.IsType<DeleteMessage>(request.Request);
                Assert.Equal(messageId, sent.MessageId);
            }
        });
    }

    [Fact]
    public void ToggleReaction_OnClient_SendsRequestOverWire()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            Guid messageId = Guid.NewGuid();
            using (adapter)
            {
                communicator.ClearSent();

                Guid id = adapter.ToggleReaction(messageId, "🎉");

                ChatClientRequestBody request = Assert.Single(communicator.ReadRequests());
                Assert.Equal(id, request.RequestId);
                ToggleReaction sent = Assert.IsType<ToggleReaction>(request.Request);
                Assert.Equal(messageId, sent.MessageId);
                Assert.Equal("🎉", sent.Emoji);
            }
        });
    }

    // ---- events -----------------------------------------------------------

    [Fact]
    public void ClientEvents_AreRaisedOnTheUiThread()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out ChatClient client);
            using (adapter)
            {
                List<ChatOperation> applied = new();
                List<(Guid, RoomResult)> completions = new();
                int resets = 0;
                List<IReadOnlySet<string>> participantUpdates = new();
                adapter.OperationApplied += op => applied.Add(op);
                adapter.RequestCompleted += (id, result) => completions.Add((id, result));
                adapter.RoomReset += () => resets++;
                adapter.ParticipantsChanged += p => participantUpdates.Add(p);

                Guid requestId = Guid.NewGuid();
                SendToClient(communicator, new ChatServerRequestResult(requestId, RoomResult.Ok));
                SendToClient(communicator, MakeBatch());
                SendToClient(communicator, new ChatServerUpdateEnvelope(
                    new ChatServerReset(), new[] { "hostuser", "alice" }));

                Assert.Single(completions);
                Assert.Equal(requestId, completions[0].Item1);
                Assert.Single(applied);
                Assert.Equal(1, resets);
                // Reset raises set participants afterwards.
                Assert.NotEmpty(participantUpdates);
            }

            _ = client;
        });
    }

    private static ChatServerUpdateEnvelope MakeBatch()
    {
        return new ChatServerUpdateEnvelope(
            new ChatServerOperationBatch(0, 1, new ChatOperation[]
            {
                new MessageAdded(
                    Guid.NewGuid(),
                    new ChatMessageWithThreads
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        SenderName = "hostuser",
                        Content = "hi"
                    })
            }),
            new[] { "hostuser", "alice" });
    }

    private static void SendToClient(RecordingCommunicator communicator, ChatServerMessage message)
    {
        communicator.SendToSubscriber(message);
    }

    [Fact]
    public void ParticipantsChanged_ReceivesIndependentSnapshot()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            using (adapter)
            {
                List<IReadOnlySet<string>> snapshots = new();
                adapter.ParticipantsChanged += p => snapshots.Add(p);

                List<string> live = new() { "hostuser", "alice" };
                communicator.SendToSubscriber(new ChatServerUpdateEnvelope(new ChatServerReset(), live));

                live.Add("intruder"); // mutating the source cannot corrupt what the UI received

                IReadOnlySet<string> snapshot = Assert.Single(snapshots);
                Assert.DoesNotContain("intruder", snapshot);
                Assert.Contains("hostuser", snapshot);
            }
        });
    }

    [Fact]
    public void HostAdapter_RaisesParticipantsChangedAndUserInfoChanged_OnControllerJoin()
    {
        RecordingController controller = new("hostuser");
        controller.AddUser("bob");
        List<IReadOnlySet<string>> participants = new();
        int userInfo = 0;
        ChatClientAdapter adapter = null!;

        DispatcherThread.Run(() => {
            adapter = HostAdapter(controller, out _, out _);
            adapter.ParticipantsChanged += p => participants.Add(p);
            adapter.UserInfoChanged += () => userInfo++;

            controller.RaiseUserJoined("carol");
        });

        DispatcherThread.Flush(); // host participants are enqueued deliberately

        DispatcherThread.Run(() => {
            using (adapter)
            {
                Assert.Single(participants);
                Assert.Contains("hostuser", participants[0]);
                Assert.Equal(1, userInfo);
            }
        });
    }

    [Fact]
    public void UserInfoChanged_FiresWithoutParticipantsChanged_OnClientAdapter()
    {
        DispatcherThread.Run(() => {
            RecordingController controller = new("alice");
            ChatClientAdapter adapter = ClientAdapter(new RecordingCommunicator(), controller, out _);
            using (adapter)
            {
                int userInfo = 0;
                int participantEvents = 0;
                adapter.UserInfoChanged += () => userInfo++;
                adapter.ParticipantsChanged += _ => participantEvents++;

                controller.RaiseUserJoined("bob", TestImages.OnePixelPng);

                Assert.Equal(1, userInfo);
                Assert.Equal(0, participantEvents);
            }
        });
    }

    // ---- disposal ---------------------------------------------------------

    [Fact]
    public void Dispose_Twice_DoesNotThrow_AndEventsAreIgnoredAfterwards()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            RecordingController controller = new("alice");
            ChatClientAdapter adapter = ClientAdapter(communicator, controller, out _);
            int updates = 0;
            adapter.ParticipantsChanged += _ => updates++;

            adapter.Dispose();
            adapter.Dispose(); // second call must be a no-op

            communicator.SendToSubscriber(MakeBatch());

            Assert.Equal(0, updates);
        });
    }

    [Fact]
    public void Operations_ThrowObjectDisposedException_AfterDispose()
    {
        DispatcherThread.Run(() => {
            RecordingCommunicator communicator = new();
            ChatClientAdapter adapter = ClientAdapter(communicator, null, out _);
            adapter.Dispose();

            Assert.Throws<ObjectDisposedException>(() => adapter.Sync());
            Assert.Throws<ObjectDisposedException>(() => adapter.Reset());
            Assert.Throws<ObjectDisposedException>(() => adapter.SendMessage("x", new Mentions()));
            Assert.Throws<ObjectDisposedException>(() => adapter.EditMessage(Guid.NewGuid(), "x"));
            Assert.Throws<ObjectDisposedException>(() => adapter.DeleteMessage(Guid.NewGuid()));
            Assert.Throws<ObjectDisposedException>(() => adapter.ToggleReaction(Guid.NewGuid(), "👍"));
        });
    }
}
