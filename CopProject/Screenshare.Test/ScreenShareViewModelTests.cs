// Author = Sumit
// Roll Number = 112301032

using Screenshare.Tests.Fakes;
using ScreenShare.Models;
using ScreenShare.ViewModels;

namespace Screenshare.Tests;

public class ScreenShareViewModelTests
{
    private static readonly TimeSpan s_feedDelay = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan s_fpsInterval = TimeSpan.FromSeconds(1);

    private readonly FakeScreenShareFeed _feed = new();
    private readonly FakeUiScheduler _ui = new();
    private readonly ManualTimeProvider _clock = new();
    private readonly List<FakeTileSurface> _surfaces = new();
    private readonly List<string?> _changed = new();
    private readonly ScreenShareViewModel _vm;

    public ScreenShareViewModelTests()
    {
        _vm = new ScreenShareViewModel(_feed, _ui, _clock, (width, height) => {
            FakeTileSurface surface = new(width, height);
            _surfaces.Add(surface);
            return surface;
        });
        _vm.PropertyChanged += (_, e) => _changed.Add(e.PropertyName);

        // the screenshare panel is on screen; forget the empty request that showing it sent
        _vm.SetActive(true);
        _feed.Requests.Clear();
    }

    private FakeUiTimer FeedTimer => _ui.Timer(s_feedDelay);

    private FakeUiTimer FpsTimer => _ui.Timer(s_fpsInterval);

    private ParticipantTile Tile(string id)
    {
        return _vm.Tiles.Single(t => t.Id == id);
    }

    private static ScreenFrame Frame(byte shade = 0)
    {
        byte[] pixels = new byte[4 * 3 * 4];
        Array.Fill(pixels, shade);
        return new ScreenFrame(4, 3, pixels);
    }

    // participants join and the UI has caught up
    private void Join(params string[] ids)
    {
        foreach (string id in ids)
        {
            _feed.RaiseJoined(id, id.ToUpperInvariant());
        }

        _ui.RunPending();
    }

    // tiles scroll into view and the 150 ms wait has passed
    private void Show(params string[] ids)
    {
        foreach (string id in ids)
        {
            _vm.TileEnteredView(Tile(id));
        }

        FeedTimer.Fire();
    }

    private void SendFrame(string id, ScreenFrame frame)
    {
        _feed.RaiseFrame(id, frame);
        _ui.RunPending();
    }

    // construction

    [Fact]
    public void Constructor_NullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenShareViewModel(null!, _ui, _clock, null));
        Assert.Throws<ArgumentNullException>(() => new ScreenShareViewModel(_feed, null!, _clock, null));
        Assert.Throws<ArgumentNullException>(() => new ScreenShareViewModel(_feed, _ui, null!, null));
    }

    [Fact]
    public void Constructor_StartsInEmptyGridWithFpsTimerRunning()
    {
        Assert.Empty(_vm.Tiles);
        Assert.Equal(ViewMode.Grid, _vm.Mode);
        Assert.True(_vm.IsGridMode);
        Assert.False(_vm.IsZoomedMode);
        Assert.Null(_vm.ZoomedTile);
        Assert.True(FpsTimer.IsRunning);
        Assert.True(FpsTimer.IsRepeating);
        Assert.False(FeedTimer.IsRunning);
        Assert.False(FeedTimer.IsRepeating);
    }

    // participants joining and leaving

    [Fact]
    public void ParticipantJoined_AddsTileOnUiThread()
    {
        _feed.RaiseJoined("aryan", "Aryan");
        Assert.Empty(_vm.Tiles);

        _ui.RunPending();

        ParticipantTile tile = Assert.Single(_vm.Tiles);
        Assert.Equal(("aryan", "Aryan"), (tile.Id, tile.Name));
    }

    [Fact]
    public void ParticipantJoined_Twice_AddsOneTile()
    {
        Join("aryan", "aryan");

        Assert.Single(_vm.Tiles);
    }

    [Fact]
    public void ParticipantLeft_RemovesTileAndUpdatesRequests()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");

        _feed.RaiseLeft("aryan");
        _ui.RunPending();

        Assert.Equal("sanjay", Assert.Single(_vm.Tiles).Id);
        Assert.Equal(new[] { "sanjay" }, _feed.Requests.Last().Keys);
    }

    [Fact]
    public void ParticipantLeft_Unknown_IsIgnored()
    {
        Join("aryan");

        _feed.RaiseLeft("nobody");
        _ui.RunPending();

        Assert.Single(_vm.Tiles);
        Assert.Empty(_feed.Requests);
    }

    [Fact]
    public void ZoomedParticipantLeaves_GoesBackToGrid()
    {
        Join("aryan", "sanjay");
        _vm.Zoom("aryan");
        _changed.Clear();

        _feed.RaiseLeft("aryan");
        _ui.RunPending();

        Assert.True(_vm.IsGridMode);
        Assert.Null(_vm.ZoomedTile);
        Assert.Contains(nameof(_vm.Mode), _changed);
    }

    [Fact]
    public void OtherParticipantLeaves_StaysZoomed()
    {
        Join("aryan", "sanjay");
        _vm.Zoom("aryan");

        _feed.RaiseLeft("sanjay");
        _ui.RunPending();

        Assert.True(_vm.IsZoomedMode);
        Assert.Equal("aryan", _vm.ZoomedTile!.Id);
    }

    // zoom

    [Fact]
    public void Zoom_ShowsOneTileAndAsksOnlyForItAtFullSize()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");
        _changed.Clear();

        _vm.Zoom("aryan");

        Assert.Equal(ViewMode.Zoomed, _vm.Mode);
        Assert.True(_vm.IsZoomedMode);
        Assert.False(_vm.IsGridMode);
        Assert.Equal("aryan", _vm.ZoomedTile!.Id);
        Assert.Equal(new Dictionary<string, int> { ["aryan"] = ScreenShareViewModel.ZoomedHeight }, _feed.Requests.Last());
        Assert.Equal(
            new[] { nameof(_vm.Mode), nameof(_vm.IsGridMode), nameof(_vm.IsZoomedMode), nameof(_vm.ZoomedTile) },
            _changed);
    }

    [Fact]
    public void Zoom_UnknownTile_IsIgnored()
    {
        _vm.Zoom("nobody");

        Assert.True(_vm.IsGridMode);
        Assert.Empty(_feed.Requests);
    }

    [Fact]
    public void Zoom_StartsFpsCountAfresh()
    {
        Join("aryan");
        Show("aryan");
        SendFrame("aryan", Frame());
        _clock.Advance(s_fpsInterval);

        _vm.Zoom("aryan");

        Assert.Equal("0.0 fps", _vm.ZoomedTile!.FpsText);
    }

    [Fact]
    public void BackToGrid_AsksForVisibleTilesAgain()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");
        _vm.Zoom("aryan");

        _vm.BackToGrid();

        Assert.True(_vm.IsGridMode);
        Assert.Null(_vm.ZoomedTile);
        Assert.Equal(new[] { "aryan", "sanjay" }, _feed.Requests.Last().Keys.Order());
    }

    // tiles scrolling in and out of view

    [Fact]
    public void TileEnteredView_WaitsBeforeAsking()
    {
        Join("aryan");

        _vm.TileEnteredView(Tile("aryan"));

        Assert.True(FeedTimer.IsRunning);
        Assert.Empty(_feed.Requests);
    }

    [Fact]
    public void TilesScrollingIn_AreAskedForInOneRequestAtGridSize()
    {
        Join("aryan", "sanjay", "aditya");

        Show("aryan", "sanjay", "aditya");

        int grid = ScreenShareViewModel.GridTileHeight;
        Dictionary<string, int> expected = new() { ["aryan"] = grid, ["sanjay"] = grid, ["aditya"] = grid };
        Assert.Equal(expected, Assert.Single(_feed.Requests));
    }

    [Fact]
    public void TileEnteredView_Again_DoesNotRestartWait()
    {
        Join("aryan");
        _vm.TileEnteredView(Tile("aryan"));

        _vm.TileEnteredView(Tile("aryan"));

        Assert.Equal(1, FeedTimer.StartCount);
    }

    [Fact]
    public void TileLeftView_StopsAskingForIt()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");

        _vm.TileLeftView(Tile("aryan"));
        FeedTimer.Fire();

        Assert.Equal(new[] { "sanjay" }, _feed.Requests.Last().Keys);
    }

    [Fact]
    public void TileLeftView_NotInView_DoesNothing()
    {
        Join("aryan");

        _vm.TileLeftView(Tile("aryan"));

        Assert.Equal(0, FeedTimer.StartCount);
    }



    // frames

    [Fact]
    public void Frame_ForVisibleTile_IsDrawnOnUiThread()
    {
        Join("aryan");
        Show("aryan");
        ScreenFrame frame = Frame();

        _feed.RaiseFrame("aryan", frame);
        Assert.Empty(_surfaces);

        _ui.RunPending();

        Assert.Same(frame.Pixels, Assert.Single(Assert.Single(_surfaces).Writes));
    }

    [Fact]
    public void Frames_FasterThanUi_OnlyNewestIsDrawn()
    {
        Join("aryan");
        Show("aryan");
        ScreenFrame newest = Frame(shade: 9);

        _feed.RaiseFrame("aryan", Frame(shade: 1));
        _feed.RaiseFrame("aryan", newest);
        Assert.Equal(1, _ui.PendingCount);

        _ui.RunPending();

        Assert.Same(newest.Pixels, Assert.Single(Assert.Single(_surfaces).Writes));
    }

    [Fact]
    public void Frame_ForTileOutOfView_IsNotDrawn()
    {
        Join("aryan");

        SendFrame("aryan", Frame());

        Assert.Empty(_surfaces);
    }

    [Fact]
    public void Frame_ForUnknownParticipant_IsIgnored()
    {
        SendFrame("nobody", Frame());

        Assert.Empty(_surfaces);
    }

    [Fact]
    public void Frame_WhileZoomed_IsDrawnOnlyForZoomedTile()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");
        _vm.Zoom("aryan");

        SendFrame("aryan", Frame());
        SendFrame("sanjay", Frame());

        FakeTileSurface surface = Assert.Single(_surfaces);
        Assert.Equal(4, surface.PixelWidth);
        Assert.Single(surface.Writes);
    }

    [Fact]
    public void FpsTimer_ShowsZoomedTilesFrameRate()
    {
        Join("aryan");
        _vm.Zoom("aryan");
        SendFrame("aryan", Frame());
        SendFrame("aryan", Frame());

        _clock.Advance(s_fpsInterval);
        FpsTimer.Fire();

        Assert.Equal("2.0 fps", _vm.ZoomedTile!.FpsText);
    }

    [Fact]
    public void FpsTimer_InGrid_DoesNothing()
    {
        Join("aryan");

        Exception? thrown = Record.Exception(FpsTimer.Fire);

        Assert.Null(thrown);
        Assert.Equal("0.0 fps", Tile("aryan").FpsText);
    }

    // the share button

    [Fact]
    public void ShareButton_StartsOff()
    {
        Assert.False(_vm.IsSharingScreen);
        Assert.Equal("Start screen sharing", _vm.ShareButtonText);
    }

    [Fact]
    public void ShareButton_On_StartsSharingAndTurnsToStop()
    {
        _vm.IsSharingScreen = true;

        Assert.Equal(1, _feed.StartCalls);
        Assert.True(_vm.IsSharingScreen);
        Assert.Equal("Stop screen sharing", _vm.ShareButtonText);
        Assert.Equal(new[] { nameof(_vm.IsSharingScreen), nameof(_vm.ShareButtonText) }, _changed);
    }

    [Fact]
    public void ShareButton_Off_StopsSharing()
    {
        _vm.IsSharingScreen = true;

        _vm.IsSharingScreen = false;

        Assert.Equal(1, _feed.StopCalls);
        Assert.False(_vm.IsSharingScreen);
        Assert.Equal("Start screen sharing", _vm.ShareButtonText);
    }

    [Fact]
    public void ShareButton_SameValue_DoesNothing()
    {
        _vm.IsSharingScreen = false;

        Assert.Equal(0, _feed.StopCalls);
        Assert.Empty(_changed);
    }

    [Fact]
    public void ShareButton_StartFails_StaysOff()
    {
        _feed.StartException = new ObjectDisposedException("node");

        _vm.IsSharingScreen = true;

        Assert.False(_vm.IsSharingScreen);
        Assert.Equal("Start screen sharing", _vm.ShareButtonText);
        // the toggle is told, so it unticks itself
        Assert.Contains(nameof(_vm.IsSharingScreen), _changed);
    }

    [Fact]
    public void ShareButton_StopFails_StaysOn()
    {
        _vm.IsSharingScreen = true;
        _feed.StopException = new InvalidOperationException("still capturing");

        _vm.IsSharingScreen = false;

        Assert.True(_vm.IsSharingScreen);
        Assert.Equal("Stop screen sharing", _vm.ShareButtonText);
    }

    [Fact]
    public void WithoutSurfaceFactoryOrListeners_StillWorks()
    {
        // the app's setup: real WinUI pictures (made only when a frame is drawn) and no view yet
        FakeUiScheduler ui = new();
        ScreenShareViewModel bare = new(_feed, ui, _clock, createSurface: null);

        _feed.RaiseJoined("aryan", "Aryan");
        ui.RunPending();
        bare.IsSharingScreen = true;

        Assert.Equal("aryan", Assert.Single(bare.Tiles).Id);
        Assert.True(bare.IsSharingScreen);
        bare.Dispose();
    }

    // shutting down

    [Fact]
    public void Dispose_EmptiesViewAndStopsListening()
    {
        Join("aryan");
        _vm.Zoom("aryan");
        _vm.IsSharingScreen = true;
        _changed.Clear();

        _vm.Dispose();

        Assert.Empty(_vm.Tiles);
        Assert.True(_vm.IsGridMode);
        Assert.False(_vm.IsSharingScreen);
        Assert.Equal("Start screen sharing", _vm.ShareButtonText);
        Assert.False(_feed.HasListeners);
        Assert.True(FeedTimer.IsDisposed);
        Assert.True(FpsTimer.IsDisposed);
        Assert.Contains(nameof(_vm.ShareButtonText), _changed);
    }

    [Fact]
    public void Dispose_Twice_IsSafe()
    {
        _vm.Dispose();
        _changed.Clear();

        _vm.Dispose();

        Assert.Empty(_changed);
    }

    [Fact]
    public void WorkQueuedBeforeDispose_IsIgnoredAfter()
    {
        Join("aryan");
        Show("aryan");
        _feed.RaiseJoined("sanjay", "Sanjay");
        _feed.RaiseLeft("aryan");
        _feed.RaiseFrame("aryan", Frame());

        _vm.Dispose();
        _ui.RunPending();

        Assert.Empty(_vm.Tiles);
        Assert.Empty(_surfaces);
    }

    [Fact]
    public void AfterDispose_ButtonsDoNothing()
    {
        _vm.Dispose();
        int requests = _feed.Requests.Count;

        _vm.IsSharingScreen = true;
        _vm.BackToGrid();

        Assert.Equal(0, _feed.StartCalls);
        Assert.Equal(requests, _feed.Requests.Count);
    }

    // the screenshare panel being hidden and shown

    [Fact]
    public void SetActive_False_UnsubscribesEverything()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");

        _vm.SetActive(false);

        Assert.Empty(_feed.Requests.Last());
    }

    [Fact]
    public void SetActive_True_AsksForVisibleTilesAgain()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");
        _vm.SetActive(false);

        _vm.SetActive(true);

        int grid = ScreenShareViewModel.GridTileHeight;
        Assert.Equal(new Dictionary<string, int> { ["aryan"] = grid, ["sanjay"] = grid }, _feed.Requests.Last());
    }

    [Fact]
    public void SetActive_True_WhileZoomed_AsksForZoomedTileOnly()
    {
        Join("aryan", "sanjay");
        Show("aryan", "sanjay");
        _vm.Zoom("aryan");
        _vm.SetActive(false);

        _vm.SetActive(true);

        Assert.True(_vm.IsZoomedMode);
        Assert.Equal(new Dictionary<string, int> { ["aryan"] = ScreenShareViewModel.ZoomedHeight }, _feed.Requests.Last());
    }

    [Fact]
    public void SetActive_SameValue_DoesNothing()
    {
        _vm.SetActive(true);

        Assert.Empty(_feed.Requests);
    }

    [Fact]
    public void WhileInactive_TilesScrollingIn_AreNotAskedFor()
    {
        Join("aryan");
        _vm.SetActive(false);

        Show("aryan");

        Assert.All(_feed.Requests, Assert.Empty);
    }

    [Fact]
    public void Frame_WhileInactive_IsNotDrawn()
    {
        Join("aryan");
        Show("aryan");
        _vm.SetActive(false);

        SendFrame("aryan", Frame());

        Assert.Empty(_surfaces);
    }

    [Fact]
    public void SetActive_AfterDispose_DoesNothing()
    {
        _vm.Dispose();
        int requests = _feed.Requests.Count;

        _vm.SetActive(false);

        Assert.Equal(requests, _feed.Requests.Count);
    }
}
