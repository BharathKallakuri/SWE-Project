// Author: Rajat
// Roll no.: 112301044

using System.Net;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Chat;
using Controller;
using Insights.AI;
using Networking;
using Utils;

namespace Chat.UI.Test;

/// <summary>
/// Owns a dedicated dispatcher thread. Activation context and the XAML framework
/// are initialized once on that thread, and all test bodies are marshalled onto it,
/// so creating WinUI objects is always legal and deterministic.
/// </summary>
internal static class DispatcherThread
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static readonly DispatcherQueueController Controller;

    static DispatcherThread()
    {
        WinRtActivationContext.EnsureActivated();
        Controller = DispatcherQueueController.CreateOnDedicatedThread();
        using ManualResetEventSlim initialized = new(false);
        Exception? error = null;
        Controller.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                WinRtActivationContext.EnsureActivated();
                // The dispatcher exists on this thread, so the XAML framework can now
                // bind to it and XAML objects can be created here.
                Microsoft.UI.Xaml.Hosting.WindowsXamlManager.InitializeForCurrentThread();
            }
            catch (Exception exception)
            {
                error = exception;
            }
            finally
            {
                initialized.Set();
            }
        });
        if (!initialized.Wait(Timeout))
        {
            throw new TimeoutException("The dispatcher thread did not initialize.");
        }

        if (error is not null)
        {
            throw error;
        }
    }

    public static DispatcherQueue Queue => Controller.DispatcherQueue;

    public static void Run(Action action) => Run(() =>
    {
        action();
        return true;
    });

    /// <summary>
    /// Drains everything queued on the dispatcher. Only call this from the
    /// test thread, between <see cref="Run"/> phases — never from inside a test body.
    /// </summary>
    public static void Flush()
    {
        Run(() => true);
    }

    public static T Run<T>(Func<T> action)
    {
        using ManualResetEventSlim done = new(false);
        T? result = default;
        Exception? error = null;

        if (!Queue.TryEnqueue(() =>
            {
                try
                {
                    result = action();
                }
                catch (Exception exception)
                {
                    error = exception;
                }
                finally
                {
                    done.Set();
                }
            }))
        {
            throw new InvalidOperationException("Could not enqueue the test body onto the dispatcher.");
        }

        if (!done.Wait(Timeout))
        {
            throw new TimeoutException("The test body did not complete on the dispatcher thread.");
        }

        if (error is not null)
        {
            throw error;
        }

        return result!;
    }
}

/// <summary>A communicator that records everything sent over the wire.</summary>
internal sealed class RecordingCommunicator : ICommunicator
{
    public int ListenPort => 0;
    public IPAddress ListenIpAddress => IPAddress.Loopback;
    public int? SubscribedModule { get; private set; }
    public IMessageListener? Subscriber { get; private set; }

    public List<SentMessage> Sent { get; } = new();

    public void AddSubscriber(int module, IMessageListener subscriber)
    {
        SubscribedModule = module;
        Subscriber = subscriber;
    }

    public void RemoveSubscriber(int module)
    {
    }

    public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
        => Sent.Add(new SentMessage(iPEndPoint, message, module, priority));

    public void BroadcastMessage(byte[] message, int module, int priority)
        => Sent.Add(new SentMessage(null, message, module, priority));

    public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
    }

    public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
    {
    }

    public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
    {
    }

    public void AddLossySubscriber(int module, IMessageListenerLossy subscriber)
    {
    }

    public void RemoveLossySubscriber(int module)
    {
    }

    public void SendMessageLossy(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
    {
    }

    public void BroadcastMessageLossy(byte[] message, int module, int priority)
    {
    }

    public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority)
    {
    }

    public List<ChatClientRequestBody> ReadRequests()
        => Sent.Select(sent =>
        {
            ChatClientRequestEnvelope envelope = JsonSerializer.Deserialize<ChatClientRequestEnvelope>(sent.Payload)!;
            return new ChatClientRequestBody(envelope.RequestId, envelope.Request, envelope.Username);
        }).ToList();

    public void ClearSent() => Sent.Clear();

    public void SendToSubscriber(ChatServerMessage message)
        => Subscriber?.OnMessageReceived(JsonSerializer.SerializeToUtf8Bytes(message));

    public sealed record SentMessage(IPEndPoint? Destination, byte[] Payload, int Module, int Priority);
}

internal sealed record ChatClientRequestBody(Guid RequestId, ChatClientRequest Request, string Username);

/// <summary>
/// A controller that knows a small fixed user list. Mirrors the shape the chat UI relies on
/// (username, gmail, profile picture) without any networking.
/// </summary>
internal sealed class RecordingController : IControllerService
{
    private readonly Dictionary<string, ControllerUser> _users = new(StringComparer.Ordinal);

    public RecordingController(string currentUsername, byte[]? currentPicture = null)
    {
        CurrentUser = new ControllerUser(
            currentUsername,
            $"{currentUsername}@example.com",
            currentPicture,
            IPAddress.Loopback.ToString(),
            12345,
            isHost: true);
        _users[CurrentUser.UserKey] = CurrentUser;
    }

    public ICommunicator? Communicator { get; set; }
    public ControllerUser? CurrentUser { get; }
    public int ListenPort => Communicator?.ListenPort ?? 0;
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;
    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;

    public void AddUser(string username, byte[]? picture = null, bool isHost = false)
    {
        _users[$"{username}@example.com"] = new ControllerUser(
            username,
            $"{username}@example.com",
            picture,
            IPAddress.Loopback.ToString(),
            20000 + _users.Count,
            isHost);
    }

    public void RaiseUserJoined(string username, byte[]? picture = null)
    {
        if (!_users.Values.Any(u => string.Equals(u.Username, username, StringComparison.Ordinal)))
        {
            AddUser(username, picture);
        }

        ControllerUser user = _users.Values.First(u => string.Equals(u.Username, username, StringComparison.Ordinal));
        UserJoined?.Invoke(this, new UserJoinedEventArgs(user, _users.Count));
    }

    public void RaiseUserLeft(string username)
    {
        ControllerUser? user = _users.Values.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.Ordinal));
        if (user is null)
        {
            return;
        }

        _users.Remove(user.UserKey);
        UserLeft?.Invoke(this, new UserLeftEventArgs(user, _users.Count));
    }

    public bool StartMeeting() => true;
    public bool EndMeeting() => true;
    public bool JoinMeeting(string hostIp, int hostPort, ControllerUser user) => true;
    public void LeaveMeeting(ControllerUser user)
    {
    }

    public int GetActiveUserCount() => _users.Count;
    public List<ControllerUser> GetActiveUsers() => _users.Values.ToList();
    public void RegisterNotificationHandler(IUserNotificationHandler handler)
    {
    }

    public void UnregisterNotificationHandler(IUserNotificationHandler handler)
    {
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Plays the server's side of the client protocol. Tracks the operation version so batches
/// are always consistent with what the client has applied.
/// </summary>
internal sealed class FakeServer
{
    private readonly RecordingCommunicator _communicator;
    private readonly ChatClient _client;
    private int _version;

    public FakeServer(RecordingCommunicator communicator, ChatClient client)
    {
        _communicator = communicator;
        _client = client;
    }

    public IReadOnlyList<string> Participants { get; set; } = new[] { "hostuser", "alice" };

    public void AddMessage(
        string sender,
        string content,
        out Guid messageId,
        Guid? threadParentMessageId = null,
        DateTimeOffset? timestamp = null,
        Guid? replyToMessageId = null)
    {
        messageId = Guid.NewGuid();
        SendOperations(new ChatOperation[]
        {
            new MessageAdded(
                messageId,
                new ChatMessageWithThreads
                {
                    Timestamp = timestamp ?? DateTimeOffset.UtcNow,
                    SenderName = sender,
                    Content = content,
                    ParentMessageId = replyToMessageId
                },
                threadParentMessageId)
        });
    }

    public void EditMessage(Guid messageId, string newContent, string actor, Guid? threadParentMessageId = null)
        => SendOperations(new ChatOperation[] { new MessageEdited(messageId, newContent, actor, threadParentMessageId) });

    public void DeleteMessage(Guid messageId, string actor, Guid? threadParentMessageId = null)
        => SendOperations(new ChatOperation[] { new MessageDeleted(messageId, actor, threadParentMessageId) });

    public void ToggleReaction(Guid messageId, string emoji, string actor, Guid? threadParentMessageId = null)
        => SendOperations(new ChatOperation[] { new ReactionToggled(messageId, emoji, actor, threadParentMessageId) });

    public void SendReset()
    {
        _version = 0;
        SendUpdate(new ChatServerReset());
    }

    public void SendRequestResult(Guid requestId, RoomResult result)
        => _communicator.SendToSubscriber(new ChatServerRequestResult(requestId, result));

    public void SendBatchWithWrongBaseVersion()
        => _communicator.SendToSubscriber(new ChatServerUpdateEnvelope(
            new ChatServerOperationBatch(99, 100, Array.Empty<ChatOperation>()),
            Participants));

    public void SendOperations(IReadOnlyList<ChatOperation> operations)
    {
        int fromVersion = _version;
        _version += operations.Count;
        SendUpdate(new ChatServerOperationBatch(fromVersion, _version, operations));
    }

    private void SendUpdate(ChatServerUpdate update)
        => _communicator.SendToSubscriber(new ChatServerUpdateEnvelope(update, Participants));
}

/// <summary>An insights provider that always returns the same string.</summary>
internal sealed class InsightsTestProvider : IInsightsProvider
{
    private readonly string _response;

    public InsightsTestProvider(string response) => _response = response;

    public string ModelName => "test";

    public Task<string> AnalyzeTrafficIncidentAsync(string textDescription) => Task.FromResult(_response);

    public Task<string> AnalyzeImageAsync(byte[] imageBytes, string prompt) => Task.FromResult(_response);

    public Task<string> GenerateChatResponseAsync(string userMessage, string? chatContext = null) => Task.FromResult(_response);
}

/// <summary>A 1x1 transparent PNG, small enough to inline in tests.</summary>
internal static class TestImages
{
    public static byte[] OnePixelPng { get; } = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
}
