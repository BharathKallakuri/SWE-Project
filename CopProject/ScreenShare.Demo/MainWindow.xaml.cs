using System.Net;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Networking;
using ScreenShare.Models;

namespace ScreenShare.Demo;

/// <summary>
/// host or join a meeting with Networking's TCP communicators, then show ScreenShare's
/// grid view. MockMeetingSession stands in for the Controller module and this window
/// for the UX module until they are ready.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly object _statsLock = new();
    private readonly Dictionary<string, int> _framesThisSecond = new();
    private CountingCommunicator? _counter;
    private MockMeetingSession? _session;
    private ScreenShareNode? _node;
    private DispatcherQueueTimer? _statsTimer;

    public MainWindow()
    {
        InitializeComponent();
        NameBox.Text = Environment.UserName;
        Closed += (_, _) => Leave();
    }

    private void Host_Click(object sender, RoutedEventArgs e)
    {
        ICommunicator server = CommunicatorFactory.CreateServerCommunicator();
        if (server.ListenPort == 0)
        {
            (server as IDisposable)?.Dispose();
            StartError.Text = "Could not start listening for connections.";
            return;
        }

        StartMeeting(server, (IDisposable)server, new IPEndPoint(server.ListenIpAddress, server.ListenPort), isHost: true);
    }

    private void Join_Click(object sender, RoutedEventArgs e)
    {
        if (!IPEndPoint.TryParse(HostAddressBox.Text.Trim(), out IPEndPoint? hostEndpoint) || hostEndpoint.Port == 0)
        {
            StartError.Text = "Enter the host's address as IP:port, as shown in the host's window.";
            return;
        }

        // the client logs instead of throwing when it cannot connect, and then has no local port
        ICommunicator client = CommunicatorFactory.CreateClientCommunicator(hostEndpoint);
        if (client.ListenPort == 0)
        {
            (client as IDisposable)?.Dispose();
            StartError.Text = $"Could not connect to {hostEndpoint}. Check the address, and that the host's firewall allows it.";
            return;
        }

        StartMeeting(client, (IDisposable)client, hostEndpoint, isHost: false);
    }

    private void StartMeeting(ICommunicator communicator, IDisposable connection, IPEndPoint hostEndpoint, bool isHost)
    {
        string name = string.IsNullOrWhiteSpace(NameBox.Text) ? (isHost ? "Host" : "Guest") : NameBox.Text.Trim();

        // the mock controller gives each participant a unique id; display names need not be unique.
        // the session closes the connection when the meeting is left.
        _counter = new CountingCommunicator(communicator, ScreenShareModule.ModuleId);
        _session = new MockMeetingSession(MockMeetingSession.NewParticipant(name, isHost), hostEndpoint, _counter, connection);

        bool useTestPattern = TestPatternCheck.IsChecked == true;
        _node = new ScreenShareNode(_session, useTestPattern ? () => new TestPatternSource(name) : null);

        var feed = new ScreenShareFeed(_node);
        feed.FrameReady += CountFrame;
        ScreenShareView.Initialize(feed);

        MeetingInfo.Text = isHost
            ? $"Hosting as {name}. Others join with: {hostEndpoint}"
            : $"Joined {hostEndpoint} as {name}";
        Title = $"ScreenShare demo - {name}{(isHost ? " (host)" : "")}";
        StartPanel.Visibility = Visibility.Collapsed;
        MeetingPanel.Visibility = Visibility.Visible;

        _statsTimer = DispatcherQueue.CreateTimer();
        _statsTimer.Interval = TimeSpan.FromSeconds(1);
        _statsTimer.Tick += (_, _) => UpdateStats();
        _statsTimer.Start();
    }

    private void Leave_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Leave()
    {
        _statsTimer?.Stop();

        // leaving ends the meeting, which shuts ScreenShare down, and only then
        // closes the shared connection, as the Controller adapter will
        _session?.Leave();
        _node?.Dispose();
    }

    // runs on ScreenShare's thread
    private void CountFrame(string streamId, ScreenFrame frame)
    {
        lock (_statsLock)
        {
            _framesThisSecond[streamId] = _framesThisSecond.GetValueOrDefault(streamId) + 1;
        }
    }

    private void UpdateStats()
    {
        Dictionary<string, int> frames;
        lock (_statsLock)
        {
            frames = new Dictionary<string, int>(_framesThisSecond);
            _framesThisSecond.Clear();
        }

        (long sent, long received) = _counter!.TakeCounts();
        string streams = frames.Count == 0
            ? "no new frames"
            : string.Join(", ", frames.Select(f => $"{ShortName(f.Key)} {f.Value} fps"));

        StatsText.Text =
            $"{streams} | in {received / 1024.0:0.0} KB/s, out {sent / 1024.0:0.0} KB/s | " +
            $"sharing: {(_node!.IsSharing ? "yes" : "no")}";
    }

    // ids are "name-abc123"; show just the name part
    private static string ShortName(string id) =>
        id.Length > 7 && id[^7] == '-' ? id[..^7] : id;
}
