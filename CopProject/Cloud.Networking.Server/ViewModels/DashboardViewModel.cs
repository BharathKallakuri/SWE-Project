// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;

namespace Cloud.Networking.Server.ViewModels;

/// <summary>
/// The view model for Dashboard
/// </summary>
public sealed class DashboardViewModel : IDisposable
{
    /// <summary>How many of the newest messages the dashboard shows.</summary>
    public const int RecentMessageCount = 50;

    /// <summary>The longest message the dashboard (and the hub) accepts.</summary>
    public const int MaxLength = MessageBroadcaster.MaxContentLength;

    private readonly IServerMonitor _monitor;
    private readonly IMessageBroadcaster _broadcaster;

    /// <summary>Initializes the dashboard view model.</summary>
    /// <param name="monitor">The monitor providing connection and message info</param>
    /// <param name="broadcaster">Stores a message and sends it to all connected clients</param>
    public DashboardViewModel(IServerMonitor monitor, IMessageBroadcaster broadcaster)
    {
        _monitor = monitor;
        _broadcaster = broadcaster;
        _monitor.Changed += OnMonitorChanged;
    }

    public event Action? Changed;

    public string Text { get; set; } = "";

    public string? Status { get; private set; }

    public bool StatusIsError { get; private set; }

    public bool IsSending { get; private set; }

    /// <summary>Connections formatted for display (rebuilt by <see cref="RefreshAsync"/>).</summary>
    public IReadOnlyList<ConnectionItem> Connections { get; private set; } = [];

    /// <summary>Newest messages formatted for display (rebuilt by <see cref="RefreshAsync"/>).</summary>
    public IReadOnlyList<MessageItem> Messages { get; private set; } = [];

    /// <summary>
    /// Whether a message can be sent. Connected clients are not required:
    /// the message is stored, so offline clients get it when they sync.
    /// </summary>
    public bool CanSend => !IsSending && !string.IsNullOrWhiteSpace(Text) && Text.Trim().Length <= MaxLength;

    /// <summary>Rebuilds the display data, then tells the view. Called on page start and on every monitor change.</summary>
    /// <returns>A task representing the asynchronous refresh.</returns>
    public async Task RefreshAsync()
    {
        Connections = [.. _monitor.Connections.Select(c => new ConnectionItem(c.ConnectionId, FormatTime(c.ConnectedAt)))];

        try
        {
            IReadOnlyList<Message> recent = await _monitor.GetRecentMessagesAsync(RecentMessageCount);

            Messages = [.. recent.Select(m => new MessageItem(
                m.Id.ToString("N")[..8],
                FormatTime(m.Timestamp),
                m.SenderId,
                m.Content,
                m.SenderId == MessageBroadcaster.ServerSenderId))];
        }
        catch (Exception ex)
        {
            Status = $"Could not load messages: {ex.Message}";
            StatusIsError = true;
        }

        Changed?.Invoke();
    }

    /// <summary>Sends the current text to all clients and updates the status.</summary>
    /// <returns>A task representing the asynchronous send operation.</returns>
    public async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        IsSending = true;
        Changed?.Invoke();

        try
        {
            await _broadcaster.BroadcastAsync(MessageBroadcaster.ServerSenderId, Text);

            int online = _monitor.ConnectionCount;
            Status = online == 0
                ? "Saved. No clients are online; they will get it when they sync."
                : $"Sent to {online} connected client(s). Offline clients get it when they sync.";
            StatusIsError = false;
            Text = "";
        }
        catch (Exception ex)
        {
            Status = $"Failed to send: {ex.Message}";
            StatusIsError = true;
        }
        finally
        {
            IsSending = false;
            Changed?.Invoke();
        }
    }

    public void Dispose()
    {
        _monitor.Changed -= OnMonitorChanged;
    }

    private static string FormatTime(DateTimeOffset time)
    {
        return time.ToLocalTime().ToString("HH:mm:ss");
    }

    private void OnMonitorChanged()
    {
        _ = RefreshAsync();
    }
}
