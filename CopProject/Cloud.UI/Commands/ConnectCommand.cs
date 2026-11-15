// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.Networking;
using Cloud.UI.Services;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Commands;

/// <summary>
/// Connects to the server URL typed by the user, starting from a clean connection,
/// then syncs the messages missed while offline.
/// When already connected, it is available once the URL differs from the connected server:
/// it then drops the current connection and connects to the new one.
/// </summary>
public sealed class ConnectCommand : NotificationCommand
{
    private const string InvalidUrlMessage = "Enter a full http:// or https:// URL, e.g. https://localhost:5001/hub";

    public ConnectCommand(NotificationServiceViewModel viewModel, NotificationService service)
        : base(viewModel, service)
    {
    }

    protected override bool IsAvailable()
    {
        string typed = ViewModel.ServerUrl.Trim();
        if (typed.Length == 0)
        {
            return false;
        }

        // not connected: connect. connected: only to switch to a different server.
        return ViewModel.State == ConnectionState.Disconnected
            || (ViewModel.IsOnline && typed != ViewModel.ConnectedUrl);
    }

    protected override async Task RunAsync()
    {
        if (!NotificationService.TryParseUrl(ViewModel.ServerUrl, out string url))
        {
            ViewModel.ErrorText = InvalidUrlMessage;
            return;
        }

        // UseServerAsync closes the current connection (if any); until we connect again we are on no server
        ViewModel.ConnectedUrl = null;
        await Service.UseServerAsync(url);

        ViewModel.State = ConnectionState.Connecting;
        await Service.ConnectAsync();
        ViewModel.State = Service.State; // shows "Connected" while the sync below runs

        // Reached only when the connection worked
        ViewModel.ConnectedUrl = url;
        ViewModel.ServerUrl = url;

        // Catch up on what was missed while offline
        await ViewModel.SyncCommand.SyncNowAsync();
    }
}
