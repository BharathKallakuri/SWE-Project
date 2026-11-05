// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cloud.Networking.Server.ViewModels;

public sealed class DashboardViewModel : IDisposable
{
    private readonly ServerMonitor _monitor;
    private readonly IHubContext<MessageHub> _hub; /* to call hub methods outside of hub we use IHubContext*/

    public DashboardViewModel(ServerMonitor monitor, IHubContext<MessageHub> hub)
    {
        _monitor = monitor;
        _hub = hub;
        _monitor.Changed += OnMonitorChanged; /* to propagate changes from monitor to VM.Changed*/
    }

    public event Action? Changed;
    public string Text { get; set; } = "";
    public string? Status { get; set; }


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
            m.Content))
        .ToList();

    public bool CanSend => !string.IsNullOrWhiteSpace(Text) && _monitor.Connections.Count > 0;

    public async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        Message message = new() {
            SenderId = "server",
            Content = Text.Trim()
        };

        await _hub.Clients.All.SendAsync(Contract.ReceiveMessage, message);
        _monitor.RecordMessage(message);

        Status = $"Sent to {_monitor.Connections.Count} connected client(s).";
        Text = "";
        Changed?.Invoke();
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
