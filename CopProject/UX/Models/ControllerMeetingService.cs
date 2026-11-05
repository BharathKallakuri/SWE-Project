using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Controller;
using SecureMeet.Shared.Models;
using SecureMeet.Shared.Modules;

namespace UX.Models;

/// <summary>
/// Integrates the Controller module (HostController/ClientController and TCP Networking)
/// with the UX layer through IMeetingService and IMeetingSession.
/// </summary>
public sealed class ControllerMeetingService : IMeetingService
{
    private IMeetingSession? _session;
    private IControllerService? _activeController;
    private bool _isHost;
    private readonly SynchronizationContext? _uiThread = SynchronizationContext.Current;

    public void Initialize(IMeetingSession session)
    {
        _session = session;
        _session.MeetingStarted += OnMeetingStarted;
        _session.MeetingEnded += OnMeetingEnded;
    }

    /// <summary>
    /// Called when the host clicks 'Start Meeting' on the Dashboard.
    /// Creates a HostController, starts listening on a TCP port, and returns (LocalIP, Port).
    /// </summary>
    public (string Id, string Port) CreateMeeting()
    {
        CleanupController();

        var currentUser = _session?.CurrentUser;
        string username = !string.IsNullOrWhiteSpace(currentUser?.UserId) ? currentUser.UserId : "Host";
        string email = !string.IsNullOrWhiteSpace(currentUser?.Email) ? currentUser.Email : $"{username}@example.com";

        string localIp = NetworkUtils.GetLocalIpAddress();
        var hostUser = new ControllerUser(username, email, null, localIp, 0, isHost: true);

        var host = ControllerFactory.CreateHost(hostUser);
        _activeController = host;
        _isHost = true;

        bool started = host.StartMeeting();
        if (!started)
        {
            Trace.TraceWarning("HostController failed to start meeting.");
        }

        string id = localIp;
        string port = host.ListenPort.ToString();

        return (id, port);
    }

    private void OnMeetingStarted(MeetingInfo info)
    {
        if (_session == null) return;

        if (info.IsHost)
        {
            if (_activeController != null)
            {
                HookControllerEvents(_activeController);
                UpdateParticipantCount(_activeController.GetActiveUserCount());
            }
        }
        else
        {
            // Client joining remote host
            CleanupController();
            _isHost = false;

            var currentUser = _session.CurrentUser;
            string username = !string.IsNullOrWhiteSpace(currentUser?.UserId) ? currentUser.UserId : "Participant";
            string email = !string.IsNullOrWhiteSpace(currentUser?.Email) ? currentUser.Email : $"{username}@example.com";

            var clientUser = new ControllerUser(username, email, null, NetworkUtils.GetLocalIpAddress(), 0, isHost: false);
            var client = ControllerFactory.CreateClient(clientUser);
            _activeController = client;

            HookControllerEvents(client);

            string targetHost = info.Id.Trim();
            if (targetHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                targetHost = "127.0.0.1";
            }
            else if (!IPAddress.TryParse(targetHost, out _))
            {
                try
                {
                    var addresses = Dns.GetHostAddresses(targetHost);
                    var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ipv4 != null)
                    {
                        targetHost = ipv4.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to resolve host '{targetHost}': {ex.Message}");
                }
            }

            if (int.TryParse(info.Port, out int hostPort))
            {
                client.JoinMeeting(targetHost, hostPort, clientUser);
                UpdateParticipantCount(client.GetActiveUserCount());
            }
            else
            {
                Trace.TraceError($"Invalid port '{info.Port}' passed to JoinMeeting.");
            }
        }
    }

    private void HookControllerEvents(IControllerService controller)
    {
        controller.UserJoined += OnUserJoined;
        controller.UserLeft += OnUserLeft;
        controller.MeetingEnded += OnRemoteMeetingEnded;
    }

    private void UnhookControllerEvents(IControllerService controller)
    {
        controller.UserJoined -= OnUserJoined;
        controller.UserLeft -= OnUserLeft;
        controller.MeetingEnded -= OnRemoteMeetingEnded;
    }

    private void OnUserJoined(object? sender, UserJoinedEventArgs e)
    {
        UpdateParticipantCount(e.NewCount);
    }

    private void OnUserLeft(object? sender, UserLeftEventArgs e)
    {
        UpdateParticipantCount(e.NewCount);
    }

    private void OnRemoteMeetingEnded(object? sender, MeetingEndedEventArgs e)
    {
        // Meeting ended remotely by the host
        if (_session != null && _session.InMeeting)
        {
            PostToUI(() =>
            {
                if (_session.InMeeting)
                {
                    _session.EndMeeting();
                }
            });
        }
    }

    private void OnMeetingEnded()
    {
        // Fired when the UI triggers EndMeeting()
        if (_activeController != null)
        {
            if (_isHost)
            {
                _activeController.EndMeeting();
            }
            else
            {
                var currentUser = _session?.CurrentUser;
                string username = !string.IsNullOrWhiteSpace(currentUser?.UserId) ? currentUser.UserId : "Participant";
                string email = !string.IsNullOrWhiteSpace(currentUser?.Email) ? currentUser.Email : $"{username}@example.com";
                var clientUser = new ControllerUser(username, email, null, string.Empty, 0, isHost: false);
                _activeController.LeaveMeeting(clientUser);
            }

            CleanupController();
        }
    }

    private void UpdateParticipantCount(int count)
    {
        if (_session != null)
        {
            PostToUI(() => _session.ParticipantCount = count);
        }
    }

    private void PostToUI(Action action)
    {
        if (_uiThread != null)
        {
            _uiThread.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    private void CleanupController()
    {
        if (_activeController != null)
        {
            UnhookControllerEvents(_activeController);
            _activeController.Dispose();
            _activeController = null;
        }
    }
}
