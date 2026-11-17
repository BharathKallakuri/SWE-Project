// Author = Sumit
// Roll Number = 112301032

using Controller;
using Controller.Models;
using Screenshare.Tests.Fakes;
using ScreenShare;
using ScreenShare.Interfaces;
using Utils;

namespace Screenshare.Tests;

// the public constructor needs the app's ControllerMeetingService and a WinUI
// ScreenShareGridView, so these tests use the internal one with a fake view and controller
public sealed class ScreenShareMeetingIntegrationTests : IDisposable
{
    private readonly FakeNetwork _network = new();
    private readonly MeetingSession _session = new();
    private readonly MeetingInfo _meeting = new("10.0.0.5", "6000", IsHost: true);
    private readonly FakeView _view = new();
    private readonly List<Action> _pendingUi = new();
    private IControllerService? _controller;
    private bool _deferUi;
    private ScreenShareMeetingIntegration? _integration;

    public ScreenShareMeetingIntegrationTests()
    {
        _controller = new FakeControllerService {
            CurrentUser = new ControllerUser("Sumit", "sumit@gmail.com", null, "10.0.0.5", 6000, true),
            Communicator = _network.Server,
        };
    }

    public void Dispose()
    {
        _integration?.Dispose();
    }

    private ScreenShareMeetingIntegration Integration()
    {
        _integration = new ScreenShareMeetingIntegration(
            () => _controller,
            _session,
            _view,
            RunOnUi,
            () => new FakeImageSource(),
            options: null);
        return _integration;
    }

    // runs UI work straight away, or holds it until RunPendingUi when _deferUi is set
    private void RunOnUi(Action action)
    {
        if (_deferUi)
        {
            _pendingUi.Add(action);
        }
        else
        {
            action();
        }
    }

    private void RunPendingUi()
    {
        foreach (Action action in _pendingUi.ToList())
        {
            action();
        }
        _pendingUi.Clear();
    }

    [Fact]
    public void Constructor_NullArgument_Throws()
    {
        Func<IControllerService?> controller = () => _controller;
        Action<Action> ui = RunOnUi;

        Assert.Throws<ArgumentNullException>(() => new ScreenShareMeetingIntegration(null!, _session, _view, ui, null, null));
        Assert.Throws<ArgumentNullException>(() => new ScreenShareMeetingIntegration(controller, null!, _view, ui, null, null));
        Assert.Throws<ArgumentNullException>(() => new ScreenShareMeetingIntegration(controller, _session, null!, ui, null, null));
        Assert.Throws<ArgumentNullException>(() => new ScreenShareMeetingIntegration(controller, _session, _view, null!, null, null));
    }

    [Fact]
    public void MeetingStart_ShowsAFeed()
    {
        Integration();

        _session.StartMeeting(_meeting);

        Assert.Single(_view.Shown);
        Assert.Equal(0, _view.Clears);
    }

    [Fact]
    public void MeetingEnd_ClearsTheView()
    {
        Integration();
        _session.StartMeeting(_meeting);

        _session.EndMeeting();

        Assert.Equal(1, _view.Clears);
    }

    [Fact]
    public void MeetingEnd_WithoutAMeeting_DoesNothing()
    {
        Integration();

        _session.EndMeeting();

        Assert.Equal(0, _view.Clears);
    }

    [Fact]
    public void SecondStart_StopsTheFirstMeetingFirst()
    {
        Integration();
        _session.StartMeeting(_meeting);

        _session.StartMeeting(_meeting);

        Assert.Equal(2, _view.Shown.Count);
        Assert.Equal(1, _view.Clears);
        Assert.NotSame(_view.Shown[0], _view.Shown[1]);
    }

    [Fact]
    public void NoController_LeavesTheViewEmpty()
    {
        _controller = null;
        Integration();

        _session.StartMeeting(_meeting);

        Assert.Empty(_view.Shown);
    }

    [Fact]
    public void MeetingEndsBeforeTheUiRuns_FeedIsNotShown()
    {
        _deferUi = true;
        Integration();
        _session.StartMeeting(_meeting);
        _session.EndMeeting();

        RunPendingUi();

        Assert.Empty(_view.Shown);
        Assert.Equal(1, _view.Clears);
    }

    [Fact]
    public void Dispose_StopsTheMeetingAndIgnoresLaterMeetings()
    {
        ScreenShareMeetingIntegration integration = Integration();
        _session.StartMeeting(_meeting);

        integration.Dispose();
        integration.Dispose();
        _session.StartMeeting(_meeting);

        Assert.Single(_view.Shown);
        Assert.Equal(1, _view.Clears);
    }

    private sealed class FakeView : IScreenShareView
    {
        public List<IScreenShareFeed> Shown { get; } = new();

        public int Clears { get; private set; }

        public void Show(IScreenShareFeed feed)
        {
            Shown.Add(feed);
        }

        public void Clear()
        {
            Clears++;
        }
    }
}
