// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server.ViewModels;

public sealed class DashboardViewModel : IDisposable
{
    private readonly IServerMonitor _monitor;
    private readonly IHubContext<MessageHub> _hub; /* to call hub methods outside of hub we use IHubContext*/
    private bool _sending;

    public DashboardViewModel(IServerMonitor monitor, IHubContext<MessageHub> hub)
    {
        _monitor = monitor;
        _hub = hub;
        _monitor.Changed += OnMonitorChanged; /* to propagate changes from monitor to VM.Changed*/
    }

    public event Action? Changed;
    public string Text { get; set; } = "";
    public string? Status { get; private set; }
    public bool StatusIsError { get; private set; }


    /* changes the format of monitors Connections to be suitable for display*/
    public IReadOnlyList<ConnectionItem> Connections => _monitor.Connections
        .Select(c => new ConnectionItem(c.ConnectionId, c.ConnectedAt.ToLocalTime().ToString("HH:mm:ss")))
        .ToList();

    /* changes the format of monitors Messages to be suitable for display */
    public IReadOnlyList<MessageItem> Messages => _monitor.Messages
        .Select(m => new MessageItem(
            m.Id.ToString("N")[..8],
            m.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
            m.SenderId,
            m.Content,
            m.SenderId == ServerMonitor.ServerSenderId))
        .ToList();

    public bool CanSend => !_sending && !string.IsNullOrWhiteSpace(Text) && _monitor.Connections.Count > 0;

    public async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        _sending = true;
        Changed?.Invoke(); // to tell view to disable the button

        try
        {
            Message message = new() {
                SenderId = ServerMonitor.ServerSenderId,
                Content = Text.Trim()
            };

            await _hub.Clients.All.SendAsync(Contract.ReceiveMessage, message);
            _monitor.RecordMessage(message);

            Status = $"Sent to {_monitor.Connections.Count} connected client(s).";
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
            _sending = false;
            Changed?.Invoke(); // re-enable the button / show the status
        }
    }

    /* to refer to the same handler */
    private void OnMonitorChanged()
    {
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _monitor.Changed -= OnMonitorChanged;
    }
}
