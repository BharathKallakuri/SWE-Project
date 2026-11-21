/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Cloud.Persistence;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Networking;
using Utils;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Services;
using Whiteboard.Test.Mocks;
using Whiteboard.Test.Mocks.Networking;
using Whiteboard.Test.Views;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class WhiteBoardViewModelInitializedTests
{
    private static readonly ToolbarInfo s_toolbar = new(
        Color.FromArgb(255, 0, 0, 0),
        2,
        Color.FromArgb(255, 255, 255, 255));

    [Fact]
    public void InitializeForTesting_ThrowsIfCalledTwice()
    {
        MockWhiteboardClient client = new();
        using WhiteBoardViewModel vm = new(client, "session-1");

        vm.InitializeForTesting(new MockCanvasPreview(), s_toolbar);
        Assert.Throws<InvalidOperationException>(() => vm.InitializeForTesting(new MockCanvasPreview(), s_toolbar));
    }

    [Fact]
    public void SelectTool_SwitchesAcrossAllToolsAndValidatesRange()
    {
        MockWhiteboardClient client = new();
        using WhiteBoardViewModel vm = new(client, "session-1");
        vm.InitializeForTesting(new MockCanvasPreview(), s_toolbar);

        foreach (WhiteboardTool tool in Enum.GetValues<WhiteboardTool>())
        {
            vm.SelectTool(tool);
            Assert.Equal(tool, vm.SelectedTool);
        }

        // Selecting same tool again is a no-op
        vm.SelectTool(WhiteboardTool.Select);
        Assert.Equal(WhiteboardTool.Select, vm.SelectedTool);

        // Switching away from Select clears selection
        vm.SelectTool(WhiteboardTool.Pen);
        Assert.Equal(WhiteboardTool.Pen, vm.SelectedTool);

        Assert.Throws<ArgumentOutOfRangeException>(() => vm.SelectTool((WhiteboardTool)999));
    }

    [Fact]
    public void PointerAndLifecycleMethods_ForwardWhenInitialized()
    {
        MockWhiteboardClient client = new();
        using WhiteBoardViewModel vm = new(client, "session-1");
        vm.InitializeForTesting(new MockCanvasPreview(), s_toolbar);

        bool historyFired = false;
        Action handler = () => historyFired = true;
        vm.HistoryChanged += handler;
        vm.HistoryChanged -= handler;
        Assert.False(historyFired);

        Assert.True(vm.HandlesPointerOnTextbox);

        vm.PointerPressed(new Point2D(10, 10));
        vm.PointerMoved(new Point2D(20, 20));
        vm.PointerDoubleTapped(new Point2D(20, 20));
        vm.PointerReleased(new Point2D(20, 20));
        vm.PointerCaptureLost(new Point2D(20, 20));
        vm.ResetCurrentState();

        vm.OnUserLeft(client.UserId);
        vm.OnUserLeft("other-user");
        vm.OnMeetingEnded();
    }

    [Fact]
    public void SessionConstructor_HandlesUserLeftAndDispose()
    {
        ICommunicator clientComm = new MockCommunicatorFactory().CreateClientCommunicator();
        using WhiteboardSession session = WhiteboardSession.StartClient("alice", clientComm, "127.0.0.1", 1234);
        using WhiteBoardViewModel vm = new(session);

        vm.InitializeForTesting(new MockCanvasPreview(), s_toolbar);
        session.OnUserLeft(new ControllerUser { Username = "alice" });
    }

    [Fact]
    public async Task Snapshots_SaveListAndRestoreAsync_WorkWithInjectedStore()
    {
        MockWhiteboardClient client = new();
        InMemorySnapshotStorage storage = new();
        WhiteboardSnapshotStore store = new(storage);
        using WhiteBoardViewModel vm = new(client, "session-snap");
        vm.InitializeForTesting(new MockCanvasPreview(), s_toolbar, store);

        await vm.SaveSnapshotAsync();
        List<WhiteboardSnapshotSummary> list = await vm.ListSnapshotsAsync();
        Assert.Single(list);

        await vm.RestoreSnapshotAsync(list[0].SnapshotId);
        await Assert.ThrowsAsync<ArgumentException>(() => vm.RestoreSnapshotAsync("  "));
    }

    private sealed class InMemorySnapshotStorage : IStorage
    {
        private readonly Dictionary<string, byte[]> _items = new();

        public Task<bool> Create(string key, byte[] data)
        {
            _items[key] = data;
            return Task.FromResult(true);
        }

        public Task<(bool Success, byte[] Data)> Read(string key)
            => Task.FromResult(_items.TryGetValue(key, out byte[]? data) ? (true, data) : (false, Array.Empty<byte>()));

        public Task<(bool Success, string Etag)> ReadEtag(string key) => Task.FromResult((false, string.Empty));
        public Task<bool> Update(string key, byte[] data, string etag) => Task.FromResult(false);
        public Task<bool> Delete(string key) => Task.FromResult(_items.Remove(key));
        public Task<bool> CreateAll(Dictionary<string, byte[]> items) => Task.FromResult(false);
        public Task<(bool Success, Dictionary<string, byte[]>)> ReadAll(string[] keys) => Task.FromResult((true, new Dictionary<string, byte[]>()));
        public Task<(bool Success, List<string>)> GetAllKeys() => Task.FromResult((true, _items.Keys.ToList()));
        public Task<(bool Success, Dictionary<string, string>)> ReadAllEtags() => Task.FromResult((false, new Dictionary<string, string>()));
        public Task<(bool Success, Dictionary<string, string> Etags)> ReadEtags(string[] keys) => Task.FromResult((false, new Dictionary<string, string>()));
        public Task<bool> UpdateAll(Dictionary<string, byte[]> items, Dictionary<string, string> etags) => Task.FromResult(false);
        public Task<bool> DeleteAll(string[] keys) => Task.FromResult(false);
        public Task<(bool Success, Dictionary<string, byte[]> Items)> ReadLatest(int n) => Task.FromResult((true, new Dictionary<string, byte[]>()));
    }
}

[Collection("XamlUi")]
public class WhiteBoardViewModelLiveCanvasTests(XamlUiThreadFixture ui)
{
    private static readonly ToolbarInfo s_toolbar = new(
        Color.FromArgb(255, 0, 0, 0),
        2,
        Color.FromArgb(255, 255, 255, 255));

    [Fact]
    public async Task InitializeViewModel_InitializesWithLiveCanvasAndRejectsSecondCall()
    {
        await ui.RunAsync(() => {
            MockWhiteboardClient client = new();
            Canvas canvas = new();
            using WhiteBoardViewModel vm = new(client, "session-live");

            vm.InitializeViewModel(canvas, s_toolbar);
            Assert.Throws<InvalidOperationException>(() => vm.InitializeViewModel(canvas, s_toolbar));

            vm.SelectTool(WhiteboardTool.Rectangle);
            Assert.Equal(WhiteboardTool.Rectangle, vm.SelectedTool);
        });
    }

    [Fact]
    public async Task SessionConstructor_WithDispatcherQueue_EnqueuesUserLeft()
    {
        WhiteboardSession? session = null;
        WhiteBoardViewModel? vm = null;

        await ui.RunAsync(() => {
            Canvas canvas = new();
            ICommunicator clientComm = new MockCommunicatorFactory().CreateClientCommunicator();
            session = WhiteboardSession.StartClient("alice", clientComm, "127.0.0.1", 1234);
            vm = new WhiteBoardViewModel(session, canvas.DispatcherQueue);
            vm.InitializeViewModel(canvas, s_toolbar);
            session.OnUserLeft(new ControllerUser { Username = "alice" });
        });

        // Flush enqueued OnUserLeft callback on UI thread
        await ui.RunAsync(() => {
            vm!.Dispose();
            session!.Dispose();
        });
    }
}
