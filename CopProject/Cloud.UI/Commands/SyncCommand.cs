// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using System.Diagnostics;
using Cloud.Networking;
using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>Fetches the messages missed since the newest one received by the previous sync.</summary>
public sealed class SyncCommand : NotificationCommand
{
    public SyncCommand(NotificationServiceViewModel viewModel, NotificationService service)
        : base(viewModel, service)
    {
    }

    protected override bool IsAvailable()
    {
        return ViewModel.State == ConnectionState.Connected;
    }

    protected override Task RunAsync()
    {
        return SyncNowAsync();
    }

    /// <summary>
    /// The sync itself, without the availability checks, so <see cref="ConnectCommand"/>
    /// (which is still busy when it syncs) can reuse it.
    /// </summary>
    internal async Task SyncNowAsync()
    {
        IReadOnlyList<Message> missed = await Service.SyncAsync(ViewModel.LastMessageTime);

        // Oldest first, so the newest ends up on top. No alerts for backlog items.
        int added = 0;
        foreach (Message message in missed.OrderBy(m => m.Timestamp))
        {
            if (ViewModel.AddNotification(message) is not null)
            {
                added++;
            }

            // only a sync moves the starting point (messages that are already shown are skipped above)
            if (message.Timestamp > ViewModel.LastMessageTime)
            {
                ViewModel.LastMessageTime = message.Timestamp;
            }
        }

        Trace.TraceInformation($"Sync finished: {added} new of {missed.Count} returned.");
        ViewModel.LastSyncText = $"Last synced {DateTime.Now:t} · {added} new";
    }
}
