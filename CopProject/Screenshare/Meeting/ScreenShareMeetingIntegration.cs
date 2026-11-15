// Author = Sumit
// Roll Number = 112301032

using System.Diagnostics;
using Controller;
using Controller.Models;
using Microsoft.UI.Dispatching;
using ScreenShare.Views;

namespace ScreenShare;

/// <summary>
/// connects ScreenShare to the app's meeting: when a meeting starts it creates ScreenShare for
/// it and shows it in the view; when the meeting ends it shuts ScreenShare down before the
/// Controller closes the connection, and empties the view.
/// </summary>
public sealed class ScreenShareMeetingIntegration : IDisposable
{
    private readonly Func<IControllerService?> _activeController;
    private readonly MeetingSession _session;
    private readonly IScreenShareView _view;
    private readonly Action<Action> _runOnUi;
    private readonly Func<IImageSource>? _imageSourceFactory;
    private readonly ScreenShareOptions? _options;
    private readonly object _lock = new();

    private ControllerMeetingSession? _meeting;
    private ResilientMeetingSession? _resilient;
    private ScreenShareNode? _node;
    private ScreenShareFeed? _feed;
    private bool _disposed;

    /// <param name="meetingService">the app's meeting service; gives the running meeting's controller</param>
    /// <param name="session">the app's meeting session; its start and end drive ScreenShare</param>
    /// <param name="view">the view in the meeting's Share panel</param>
    /// <param name="options">tuning; defaults to ScreenShareOptions' defaults</param>
    /// <exception cref="ArgumentNullException">an argument is null</exception>
    // not covered by unit tests, nor are the two helpers below: they need the app's
    // ControllerMeetingService and a WinUI view; the tests use the internal constructor
    public ScreenShareMeetingIntegration(ControllerMeetingService meetingService, MeetingSession session, ScreenShareGridView view, ScreenShareOptions? options = null)
        : this(
            ActiveControllerOf(meetingService),
            session,
            view,
            RunOnDispatcher(view),
            imageSourceFactory: null,
            options)
    {
    }

    /// <param name="runOnUi">runs an action on the view's thread</param>
    internal ScreenShareMeetingIntegration(Func<IControllerService?> activeController,
        MeetingSession session,
        IScreenShareView view,
        Action<Action> runOnUi,
        Func<IImageSource>? imageSourceFactory,
        ScreenShareOptions? options)
    {
        ArgumentNullException.ThrowIfNull(activeController);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(runOnUi);

        _activeController = activeController;
        _session = session;
        _view = view;
        _runOnUi = runOnUi;
        _imageSourceFactory = imageSourceFactory;
        _options = options;

        _session.MeetingStarted += OnMeetingStarted;
        _session.MeetingEnding += OnMeetingEnding;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        Trace.TraceInformation("[Screenshare] ScreenShareMeetingIntegration.Dispose: no longer following meetings.");
        _session.MeetingStarted -= OnMeetingStarted;
        _session.MeetingEnding -= OnMeetingEnding;
        StopMeeting();
    }

    private static Func<IControllerService?> ActiveControllerOf(ControllerMeetingService meetingService)
    {
        ArgumentNullException.ThrowIfNull(meetingService);

        return () => meetingService.ActiveController;
    }

    private static Action<Action> RunOnDispatcher(ScreenShareGridView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        DispatcherQueue ui = view.DispatcherQueue;
        return action => {
            if (ui.HasThreadAccess)
            {
                action();
            }
            else
            {
                ui.TryEnqueue(() => action());
            }
        };
    }

    private void OnMeetingStarted(MeetingInfo info)
    {
        Trace.TraceInformation(
            "[Screenshare] ScreenShareMeetingIntegration.OnMeetingStarted: meeting at {0}:{1} started; starting ScreenShare.",
            info.Id,
            info.Port);

        // a meeting that was not ended properly is shut down first
        StopMeeting();

        ScreenShareFeed feed;
        lock (_lock)
        {
            // only when Dispose runs on another thread while a meeting is starting, so
            // tests can't reach it reliably
            if (_disposed)
            {
                return;
            }

            ControllerMeetingSession? meeting = null;
            ResilientMeetingSession? resilient = null;
            ScreenShareNode? node = null;
            try
            {
                IControllerService controller = _activeController()
                    ?? throw new InvalidOperationException("the meeting has no controller");
                meeting = new ControllerMeetingSession(controller, _session, info);
                resilient = new ResilientMeetingSession(meeting);
                node = new ScreenShareNode(resilient, _imageSourceFactory, _options);
                feed = new ScreenShareFeed(node);
            }
            catch (Exception e)
            {
                // ScreenShare failing must not stop the meeting; the Share panel stays empty
                Trace.TraceError("[Screenshare] ScreenShareMeetingIntegration.OnMeetingStarted: could not start for this meeting: {0}", e.Message);
                node?.Dispose();
                resilient?.Dispose();
                meeting?.Dispose();
                return;
            }

            _meeting = meeting;
            _node = node;
            _feed = feed;
        }

        _runOnUi(() => {
            // skip if the meeting already ended before the UI got here
            if (IsCurrentFeed(feed))
            {
                _view.Show(feed);
                Trace.TraceInformation("[Screenshare] ScreenShareMeetingIntegration: ScreenShare is running; the Share panel shows it.");
            }
            else
            {
                Trace.TraceInformation("[Screenshare] ScreenShareMeetingIntegration: the meeting ended before the Share panel could show it.");
            }
        });
    }

    private void OnMeetingEnding()
    {
        StopMeeting();
    }

    private bool IsCurrentFeed(ScreenShareFeed feed)
    {
        lock (_lock)
        {
            return ReferenceEquals(_feed, feed);
        }
    }

    /// <summary>
    /// shuts ScreenShare down right away, on the calling thread, so it is done before the
    /// Controller closes the connection; only emptying the view waits for the UI thread
    /// </summary>
    private void StopMeeting()
    {
        ControllerMeetingSession? meeting;
        ResilientMeetingSession? resilient;
        ScreenShareNode? node;
        ScreenShareFeed? feed;
        lock (_lock)
        {
            meeting = _meeting;
            resilient = _resilient;
            node = _node;
            feed = _feed;
            _meeting = null;
            _resilient = null;
            _node = null;
            _feed = null;
        }

        if (node == null)
        {
            return;
        }

        feed?.Dispose();
        node.Dispose();
        resilient?.Dispose();
        meeting?.Dispose();
        _runOnUi(_view.Clear);
        Trace.TraceInformation("[Screenshare] ScreenShareMeetingIntegration.StopMeeting: ScreenShare stopped and the Share panel cleared.");
    }
}
