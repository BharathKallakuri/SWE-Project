using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UI.Demo.Services;

namespace UI.Demo;

public partial class App : Application
{
    private DemoHost? _demoHost;
    private Window? _startupErrorWindow;

    private int _nextParticipantNumber = 1;
    private int _openWindowCount;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // One authoritative ChatServer is shared by every demo window.
            _demoHost = new DemoHost("Chat UI Test Meeting");
            OpenParticipant("Officer 1");
        }
        catch (Exception exception)
        {
            _demoHost?.Dispose();
            _demoHost = null;
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
        if (_demoHost is null)
        {
            throw new InvalidOperationException(
                "The demo ChatServer has not been initialized.");
        }

        DispatcherQueue dispatcherQueue =
            DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException(
                "The UI DispatcherQueue is unavailable.");

        DemoParticipantSession session =
            _demoHost.CreateParticipant(username, dispatcherQueue);

        try
        {
            MainWindow window = new(session, OpenAnotherParticipant, _demoHost.ServerEndpoint);

            window.Closed += (_, _) =>
            {
                _openWindowCount--;

                if (_openWindowCount == 0)
                {
                    _demoHost?.Dispose();
                    _demoHost = null;
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
