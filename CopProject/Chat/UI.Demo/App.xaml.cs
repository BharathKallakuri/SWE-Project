using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Net;
using UI.Demo.Services;

namespace UI.Demo;

public partial class App : Application
{
    private DemoHost? _demoHost;
    private DemoClient? _demoClient;
    private ConnectionWindow? _connectionWindow;
    private Window? _startupErrorWindow;
    private Func<string, DispatcherQueue, DemoParticipantSession>? _participantFactory;
    private IPEndPoint? _serverEndpoint;
    private bool _canOpenAnotherParticipant;

    private int _nextParticipantNumber = 1;
    private int _openWindowCount;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _connectionWindow = new ConnectionWindow();
        _connectionWindow.HostRequested += StartHost;
        _connectionWindow.JoinRequested += StartClient;
        _connectionWindow.Activate();
    }

    private void StartHost(string username, IPAddress bindAddress, int port)
    {
        try
        {
            _demoHost = new DemoHost(
                "Chat UI Test Meeting",
                username,
                bindAddress,
                port);
            _participantFactory = _demoHost.CreateParticipant;
            _serverEndpoint = _demoHost.ServerEndpoint;
            _canOpenAnotherParticipant = true;
            _connectionWindow?.Close();
            OpenParticipant(username);
        }
        catch (Exception exception)
        {
            _demoHost?.Dispose();
            _demoHost = null;
            ShowStartupError(exception);
        }
    }

    private void StartClient(string username, IPEndPoint serverEndpoint)
    {
        try
        {
            _demoClient = new DemoClient("Chat UI Test Meeting", serverEndpoint);
            _participantFactory = _demoClient.CreateParticipant;
            _serverEndpoint = serverEndpoint;
            _canOpenAnotherParticipant = false;
            _connectionWindow?.Close();
            OpenParticipant(username);
        }
        catch (Exception exception)
        {
            _demoClient?.Dispose();
            _demoClient = null;
            ShowStartupError(exception);
        }
    }

    private void ShowStartupError(Exception exception)
    {
        System.Diagnostics.Debug.WriteLine(exception);

        _startupErrorWindow = new Window
        {
            Title = "Chat UI Demo - Startup Error",
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = exception.ToString(),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    IsTextSelectionEnabled = true
                },
                Padding = new Thickness(20)
            }
        };

        _startupErrorWindow.Activate();
    }

    /// <summary>
    /// Opens the next independent participant window.
    /// </summary>
    private void OpenAnotherParticipant()
    {
        if (!_canOpenAnotherParticipant)
            return;

        _nextParticipantNumber++;
        string username = $"Officer {_nextParticipantNumber}";

        try
        {
            OpenParticipant(username);
        }
        catch (Exception exception)
        {
            ShowStartupError(exception);
        }
    }

    /// <summary>
    /// Creates one independent TCP client/session and a window hosting ChatPanel.
    /// </summary>
    private void OpenParticipant(string username)
    {
        if (_participantFactory is null || _serverEndpoint is null)
        {
            throw new InvalidOperationException(
                "The chat demo connection has not been initialized.");
        }

        DispatcherQueue dispatcherQueue =
            DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException(
                "The UI DispatcherQueue is unavailable.");

        DemoParticipantSession session =
            _participantFactory(username, dispatcherQueue);

        try
        {
            MainWindow window = new(
                session,
                _canOpenAnotherParticipant ? OpenAnotherParticipant : null,
                _serverEndpoint);

            window.Closed += (_, _) =>
            {
                _openWindowCount--;

                if (_openWindowCount == 0)
                {
                    _demoHost?.Dispose();
                    _demoHost = null;
                    _demoClient?.Dispose();
                    _demoClient = null;
                }
            };

            window.Activate();
            _openWindowCount++;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }
}
