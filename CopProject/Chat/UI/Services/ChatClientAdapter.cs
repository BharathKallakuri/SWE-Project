using Chat;
using Microsoft.UI.Dispatching;

namespace UI.Services;

/// <summary>
/// Connects the Chat backend to the UI layer.
/// Handles backend events and dispatches notifications to the UI thread.
/// </summary>
public sealed class ChatClientAdapter : IDisposable
{
    private readonly ChatClient _client;
    private readonly DispatcherQueue _dispatcherQueue;

    private int _disposed;

    public ChatClientAdapter(
        ChatClient client,
        DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        _client = client;
        _dispatcherQueue = dispatcherQueue;

        // Subscribe to backend events.
        _client.OperationApplied += OnOperationApplied;
        _client.RequestCompleted += OnRequestCompleted;
        _client.RoomReset += OnRoomReset;
        _client.ParticipantsChanged += OnParticipantsChanged;
    }

    // Initial information from the connected chatroom.

    public string Username => _client.Username;

    public string HostName => _client.Chatroom.HostName;

    public bool IsHost => string.Equals(Username, HostName, StringComparison.Ordinal);

    public IReadOnlySet<string> Participants =>
        _client.Chatroom.Participants;

    // Events exposed to the ViewModels.
    // These events are always raised on the UI thread.

    public event Action<ChatOperation>? OperationApplied;

    public event Action<Guid, RoomResult>? RequestCompleted;

    public event Action? RoomReset;

    public event Action<IReadOnlySet<string>>? ParticipantsChanged;

    // Backend event handlers.

    private void OnOperationApplied(ChatOperation operation)
    {
        DispatchToUI(() => {
            OperationApplied?.Invoke(operation);
        });
    }

    private void OnRequestCompleted(Guid requestId, RoomResult result)
    {
        DispatchToUI(() => {
            RequestCompleted?.Invoke(requestId, result);
        });
    }

    private void OnRoomReset()
    {
        DispatchToUI(() => {
            RoomReset?.Invoke();
        });
    }

    private void OnParticipantsChanged(IReadOnlySet<string> participants)
    {
        // Create an independent snapshot before dispatching.
        var snapshot = new HashSet<string>(participants);

        DispatchToUI(() => {
            ParticipantsChanged?.Invoke(snapshot);
        });
    }

    // Dispatcher implementation.

    /// <summary>
    /// Schedules work on the UI thread. The callback executes immediately when
    /// called from the UI thread, otherwise it is queued through DispatcherQueue.
    /// </summary>
    public bool TryDispatchToUI(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (Volatile.Read(ref _disposed) != 0)
            return false;

        if (_dispatcherQueue.HasThreadAccess)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return false;

            callback();
            return true;
        }

        return _dispatcherQueue.TryEnqueue(() =>
        {
            // The adapter might have been disposed while the callback was queued.
            if (Volatile.Read(ref _disposed) == 0)
                callback();
        });
    }

    private void DispatchToUI(Action callback)
    {
        if (!TryDispatchToUI(callback))
        {
            System.Diagnostics.Trace.WriteLine(
                $"[ChatClientAdapter:{Username}] The UI dispatcher is unavailable; an update was not applied.");
        }
    }

    // Chat operations exposed to the ViewModels.

    public Guid Sync()
    {
        ThrowIfDisposed();
        return _client.Sync();
    }

    public Guid Reset()
    {
        ThrowIfDisposed();
        return _client.Reset();
    }

    public Guid SendMessage(
        string content,
        Mentions mentions,
        Guid? threadParentMessageId = null,
        Guid? replyToMessageId = null)
    {
        ThrowIfDisposed();

        return _client.SendMessage(
            content,
            mentions,
            threadParentMessageId,
            replyToMessageId);
    }

    public Guid EditMessage(
        Guid messageId,
        string newContent,
        Guid? threadParentMessageId = null)
    {
        ThrowIfDisposed();

        return _client.EditMessage(
            messageId,
            newContent,
            threadParentMessageId);
    }

    public Guid DeleteMessage(
        Guid messageId,
        Guid? threadParentMessageId = null)
    {
        ThrowIfDisposed();

        return _client.DeleteMessage(
            messageId,
            threadParentMessageId);
    }

    public Guid ToggleReaction(
        Guid messageId,
        string emoji,
        Guid? threadParentMessageId = null)
    {
        ThrowIfDisposed();

        return _client.ToggleReaction(
            messageId,
            emoji,
            threadParentMessageId);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposed) != 0,
            this);
    }

    // Unsubscribe when the adapter is no longer needed.

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _client.OperationApplied -= OnOperationApplied;
        _client.RequestCompleted -= OnRequestCompleted;
        _client.RoomReset -= OnRoomReset;
        _client.ParticipantsChanged -= OnParticipantsChanged;
    }
}
