// Author = Sumit
// Roll Number = 112301032

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using ScreenShare.Interfaces;
using ScreenShare.Models;

namespace ScreenShare.ViewModels;

public sealed class ScreenShareViewModel : INotifyPropertyChanged, IDisposable
{
    // grid tiles are 320x180, so they get the sharer's small stream; a zoomed tile asks for
    // more than any stream offers, so it gets the full one (ScreenShareOptions.MaxHeight)
    internal const int GridTileHeight = 180;
    internal const int ZoomedHeight = 1080;

    private readonly IScreenShareFeed _feed;
    private readonly IUiScheduler _ui;
    private readonly IUiTimer _feedTimer;
    private readonly IUiTimer _fpsTimer;
    private readonly TimeProvider _time;
    private readonly long _startedAt;
    private readonly Func<int, int, ITileSurface>? _createSurface;
    private readonly Dictionary<string, ParticipantTile> _byId = new();
    private readonly HashSet<string> _onScreen = new();   // tiles currently in the viewport
    private string? _zoomedId;
    private bool _isSharingScreen;
    private bool _disposed;

    // latest frame per stream not yet shown; only the newest is drawn if the UI falls behind
    private readonly Dictionary<string, ScreenFrame> _pendingFrames = new();
    private readonly object _pendingLock = new();

    public ObservableCollection<ParticipantTile> Tiles { get; } = new();

    public ViewMode Mode { get; private set; } = ViewMode.Grid;
    public bool IsGridMode => Mode == ViewMode.Grid;
    public bool IsZoomedMode => Mode == ViewMode.Zoomed;
    public ParticipantTile? ZoomedTile =>
        _zoomedId != null && _byId.TryGetValue(_zoomedId, out ParticipantTile? t) ? t : null;



    public event PropertyChangedEventHandler? PropertyChanged;

    // not covered by unit tests: a DispatcherQueue only exists inside a running WinUI app
    public ScreenShareViewModel(IScreenShareFeed feed, DispatcherQueue ui)
        : this(feed, new DispatcherQueueScheduler(ui), TimeProvider.System, createSurface: null)
    {
    }

    /// <param name="ui">runs work and timers on the UI thread</param>
    /// <param name="timeProvider">clock for the fps counter</param>
    /// <param name="createSurface">makes each tile's picture; null for a WinUI bitmap</param>
    /// <exception cref="ArgumentNullException">feed, ui or timeProvider is null</exception>
    internal ScreenShareViewModel(IScreenShareFeed feed, IUiScheduler ui, TimeProvider timeProvider,
        Func<int, int, ITileSurface>? createSurface)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _feed = feed;
        _ui = ui;
        _time = timeProvider;
        _startedAt = timeProvider.GetTimestamp();
        _createSurface = createSurface;

        // Collects bursts of scroll events into one RequestFeeds call
        _feedTimer = _ui.CreateTimer(TimeSpan.FromMilliseconds(150), isRepeating: false, SendFeedRequests);

        // refreshes the zoomed tile's fps once a second
        _fpsTimer = _ui.CreateTimer(TimeSpan.FromSeconds(1), isRepeating: true, OnFpsTimerTick);
        _fpsTimer.Start();

        // Feed events can arrive on any thread, so hop to the UI thread first.
        _feed.ParticipantJoined += OnParticipantJoined;
        _feed.ParticipantLeft += OnParticipantLeft;
        _feed.FrameReady += QueueFrame;
    }

    /// <summary>
    /// stops listening to the feed and empties the grid, e.g. when the meeting ends.
    /// call on the UI thread.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Trace.TraceInformation("[Screenshare] ScreenShareViewModel.Dispose: grid emptied.");
        _feed.ParticipantJoined -= OnParticipantJoined;
        _feed.ParticipantLeft -= OnParticipantLeft;
        _feed.FrameReady -= QueueFrame;
        _feedTimer.Dispose();
        _fpsTimer.Dispose();

        lock (_pendingLock)
        {
            _pendingFrames.Clear();
        }

        Tiles.Clear();
        _byId.Clear();
        _onScreen.Clear();
        _zoomedId = null;
        Mode = ViewMode.Grid;
        _isSharingScreen = false;
        RaiseModeChanged();
        Raise(nameof(IsSharingScreen), nameof(ShareButtonText));
    }

    private TimeSpan Elapsed => _time.GetElapsedTime(_startedAt);

    private void OnFpsTimerTick()
    {
        ZoomedTile?.SampleFps(Elapsed);
    }

    private void OnParticipantJoined(string id, string name)
    {
        _ui.Post(() => AddParticipant(id, name));
    }

    private void OnParticipantLeft(string id)
    {
        _ui.Post(() => RemoveParticipant(id));
    }

    // ----- Called by the View -----
    public void Zoom(string id)
    {
        if (!_byId.ContainsKey(id))
        {
            return;
        }

        Trace.TraceInformation("[Screenshare] ScreenShareViewModel.Zoom: zoomed into {0}.", _byId[id].Name);
        _zoomedId = id;
        _byId[id].ResetFps(Elapsed);   // count only frames drawn while zoomed
        Mode = ViewMode.Zoomed;
        RaiseModeChanged();
        SendFeedRequests();
    }

    public void BackToGrid()
    {
        Trace.TraceInformation("[Screenshare] ScreenShareViewModel.BackToGrid: back to the grid.");
        _zoomedId = null;
        Mode = ViewMode.Grid;
        RaiseModeChanged();
        SendFeedRequests();
    }

    public void TileEnteredView(ParticipantTile tile)
    {
        if (_onScreen.Add(tile.Id))
        {
            ScheduleFeedRequests();
        }
    }

    public void TileLeftView(ParticipantTile tile)
    {
        if (_onScreen.Remove(tile.Id))
        {
            ScheduleFeedRequests();
        }
    }

    // ----- Called from feed events  -----
    private void AddParticipant(string id, string name)
    {
        if (_disposed || _byId.ContainsKey(id))
        {
            return;
        }

        ParticipantTile tile = _createSurface == null
            ? new ParticipantTile(id, name)
            : new ParticipantTile(id, name, _createSurface);
        _byId[id] = tile;
        Tiles.Add(tile);
        Trace.TraceInformation("[Screenshare] ScreenShareViewModel.AddParticipant: tile added for {0}; {1} tile(s).", name, Tiles.Count);             // if it lands in view, ElementPrepared fires -> TileEnteredView
    }

    private void RemoveParticipant(string id)
    {
        if (_disposed || !_byId.Remove(id, out ParticipantTile? tile))
        {
            return;
        }

        Tiles.Remove(tile);
        _onScreen.Remove(id);
        Trace.TraceInformation("[Screenshare] ScreenShareViewModel.RemoveParticipant: tile removed for {0}; {1} tile(s).", tile.Name, Tiles.Count);

        if (_zoomedId == id)
        {
            _zoomedId = null;
            Mode = ViewMode.Grid;
            RaiseModeChanged();
        }
        SendFeedRequests();
    }

    // Runs on the feed's thread. Keeps only the newest frame per stream and schedules one
    // UI update for it, so a slow UI skips frames instead of queueing them up.
    private void QueueFrame(string id, ScreenFrame frame)
    {
        bool alreadyScheduled;
        lock (_pendingLock)
        {
            alreadyScheduled = _pendingFrames.ContainsKey(id);
            _pendingFrames[id] = frame;
        }

        if (!alreadyScheduled)
        {
            _ui.Post(() => OnFrame(id));
        }
    }

    private void OnFrame(string id)
    {
        ScreenFrame? frame;
        lock (_pendingLock)
        {
            _pendingFrames.Remove(id, out frame);
        }

        if (_disposed)
        {
            return;
        }

        bool shown = IsZoomedMode ? id == _zoomedId : _onScreen.Contains(id);
        if (frame != null && shown && _byId.TryGetValue(id, out ParticipantTile? tile))
        {
            tile.UpdateFrame(frame);
        }
    }

    // ------ User contoles --------

    public bool IsSharingScreen
    {
        get => _isSharingScreen;
        set {
            if (_disposed || _isSharingScreen == value)
            {
                return;
            }

            try
            {
                if (value)
                {
                    _feed.StartSharing();
                }
                else
                {
                    _feed.StopSharing();
                }
            }
            catch (Exception e)
            {
                // e.g. the meeting already ended; leave the toggle showing what is really happening
                Trace.TraceError("[Screenshare] ScreenShareViewModel.IsSharingScreen: could not {0} sharing: {1}", value ? "start" : "stop", e.Message);
            }

            _isSharingScreen = _feed.IsSharing;
            Trace.TraceInformation("[Screenshare] ScreenShareViewModel.IsSharingScreen: sharing is {0}.", _isSharingScreen ? "on" : "off");
            Raise(nameof(IsSharingScreen), nameof(ShareButtonText));
        }
    }

    /// <summary>the share button's label, which follows IsSharingScreen</summary>
    public string ShareButtonText => _isSharingScreen ? "Stop screen sharing" : "Start screen sharing";


    // ----- Feed requests -----
    private void ScheduleFeedRequests()
    {
        _feedTimer.Stop();
        _feedTimer.Start();          // restarts the 150 ms wait
    }

    private void SendFeedRequests()
    {
        _feedTimer.Stop();
        if (_disposed)
        {
            return;
        }

        Dictionary<string, int> wanted = IsZoomedMode && _zoomedId != null
            ? new() { [_zoomedId] = ZoomedHeight }
            : _onScreen.ToDictionary(id => id, _ => GridTileHeight);
        _feed.RequestFeeds(wanted);
    }

    // ----- Notifications -----
    private void RaiseModeChanged()
    {
        Raise(nameof(Mode), nameof(IsGridMode), nameof(IsZoomedMode), nameof(ZoomedTile));
    }

    private void Raise(params string[] names)
    {
        foreach (string n in names)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }
    }
}
