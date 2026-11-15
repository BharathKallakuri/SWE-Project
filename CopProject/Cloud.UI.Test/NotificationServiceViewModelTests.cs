// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.UI.Models;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Test;

[TestClass]
public class NotificationServiceViewModelTests
{
    [TestMethod]
    public void InitialStateIsDisconnectedAndEmpty()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;

        Assert.AreEqual(ConnectionState.Disconnected, vm.State);
        Assert.AreEqual("Disconnected", vm.StateText);
        Assert.AreEqual("Not connected. You won't receive notifications.", vm.StateHint);
        Assert.AreEqual(2, vm.ServerPresets.Count);
        Assert.AreSame(vm.ServerPresets[1], vm.SelectedPreset, "the more powerful server is the default");
        Assert.AreEqual(vm.ServerPresets[1].Url, vm.ServerUrl);
        Assert.AreEqual(string.Empty, vm.MessageText);
        Assert.IsNull(vm.ErrorText);
        Assert.IsFalse(vm.HasError);
        Assert.IsTrue(vm.IsEmpty);
        Assert.AreEqual("Not synced yet", vm.LastSyncText);
    }

    [TestMethod]
    public void InitialCommandAvailabilityOnlyConnectIsAvailable()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;

        Assert.IsTrue(vm.ConnectCommand.CanExecute(null), "a default server URL is already filled in");
        Assert.IsFalse(vm.DisconnectCommand.CanExecute(null));
        Assert.IsFalse(vm.SendCommand.CanExecute(null));
        Assert.IsFalse(vm.SyncCommand.CanExecute(null));
        Assert.IsFalse(vm.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public void TextChangesRaiseCanExecuteChanged()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;
        int connectRaised = 0;
        int sendRaised = 0;
        vm.ConnectCommand.CanExecuteChanged += (_, _) => connectRaised++;
        vm.SendCommand.CanExecuteChanged += (_, _) => sendRaised++;

        vm.ServerUrl = ViewModelHarness.Url;
        vm.MessageText = "hi";

        Assert.AreEqual(1, connectRaised);
        Assert.AreEqual(1, sendRaised);
    }

    [TestMethod]
    public async Task ConnectRaisesStatePropertyNotifications()
    {
        ViewModelHarness h = new();
        List<string?> names = new();
        h.ViewModel.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        await h.ConnectAsync();

        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.State));
        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.StateText));
        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.StateHint));
    }

    [TestMethod]
    public async Task ConnectSuccessConnectsAndNormalisesUrl()
    {
        ViewModelHarness h = new();

        await h.ConnectAsync("  http://example.com  ");

        Assert.AreEqual(1, h.Messengers.Count);
        Assert.AreEqual("http://example.com/", h.ViewModel.ServerUrl);
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
        Assert.AreEqual("Connected", h.ViewModel.StateText);
        Assert.AreEqual("Listening for notifications.", h.ViewModel.StateHint);
        Assert.IsFalse(h.ViewModel.HasError);
        StringAssert.EndsWith(h.ViewModel.LastSyncText, "· 0 new", "connecting also syncs");
    }

    [TestMethod]
    public async Task ConnectInvalidUrlShowsErrorAndDoesNotCreateMessenger()
    {
        ViewModelHarness h = new();

        await h.ConnectAsync("ftp://nope");

        Assert.AreEqual(0, h.Messengers.Count);
        Assert.IsTrue(h.ViewModel.HasError);
        StringAssert.Contains(h.ViewModel.ErrorText, "http://");
        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
    }

    [TestMethod]
    public async Task ConnectFailureShowsErrorAndStaysDisconnected()
    {
        NotificationServiceViewModel vm = new(url => TestMessenger.Create(connectError: new IOException("refused")).Object, ViewModelHarness.RunNow) {
            ServerUrl = ViewModelHarness.Url
        };

        await vm.ConnectCommand.ExecuteAsync();

        Assert.AreEqual("refused", vm.ErrorText);
        Assert.IsTrue(vm.HasError);
        Assert.AreEqual(ConnectionState.Disconnected, vm.State);
        Assert.IsTrue(vm.ConnectCommand.CanExecute(null), "the user can retry");
    }

    [TestMethod]
    public async Task ConnectClearsPreviousError()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync("ftp://nope");
        Assert.IsTrue(h.ViewModel.HasError);

        await h.ConnectAsync();

        Assert.IsNull(h.ViewModel.ErrorText);
    }

    [TestMethod]
    public async Task ConnectWhileRunningIsBusyAndAllCommandsUnavailable()
    {
        TaskCompletionSource gate = new();
        Mock<IMessenger>? mock = null;
        NotificationServiceViewModel vm = new(url => (mock = TestMessenger.Create(connectGate: gate.Task)).Object, ViewModelHarness.RunNow) {
            ServerUrl = ViewModelHarness.Url,
            MessageText = "hello"
        };

        Task running = vm.ConnectCommand.ExecuteAsync();

        Assert.AreEqual(ConnectionState.Connecting, vm.State);
        Assert.AreEqual("Connecting…", vm.StateText);
        Assert.AreEqual("Connecting to the server…", vm.StateHint);
        Assert.IsFalse(vm.ConnectCommand.CanExecute(null));
        Assert.IsFalse(vm.DisconnectCommand.CanExecute(null));
        Assert.IsFalse(vm.SendCommand.CanExecute(null));
        Assert.IsFalse(vm.SyncCommand.CanExecute(null));

        gate.SetResult();
        await running;

        Assert.AreEqual(ConnectionState.Connected, vm.State);
        mock!.Verify(m => m.ConnectAsync(), Times.Once);
    }

    [TestMethod]
    public async Task ConnectAfterDisconnectToSameServerReusesMessengerAndKeepsNotifications()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.RaiseMessage(TestData.Msg("keep me"));
        await h.ViewModel.DisconnectCommand.ExecuteAsync();

        await h.ConnectAsync();

        Assert.AreEqual(1, h.Messengers.Count);
        h.Current.Verify(m => m.DisconnectAsync(), Times.Once);
        h.Current.Verify(m => m.ConnectAsync(), Times.Exactly(2));
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
        Assert.AreEqual(1, h.ViewModel.Notifications.Count);
    }

    [TestMethod]
    public async Task DisconnectClosesConnectionAndEnablesConnect()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        await h.ViewModel.DisconnectCommand.ExecuteAsync();

        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
        Assert.IsTrue(h.ViewModel.ConnectCommand.CanExecute(null));
        Assert.IsFalse(h.ViewModel.DisconnectCommand.CanExecute(null));
        Assert.IsFalse(h.ViewModel.SendCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ConnectToDifferentServerResetsDeduplicationAndSyncStart()
    {
        ViewModelHarness h = new();
        Guid id = Guid.NewGuid();
        await h.ConnectAsync("https://a.example/hub");
        h.Current.SyncReturns(TestData.Msg("x", TestData.T0.AddHours(5), id));
        await h.ViewModel.SyncCommand.ExecuteAsync(); // moves the sync start point to T0 + 5h
        await h.ViewModel.DisconnectCommand.ExecuteAsync();

        await h.ConnectAsync("https://b.example/hub");
        h.Current.RaiseMessage(TestData.Msg("same id on B", TestData.T0, id));

        h.Current.Verify(m => m.SyncAsync(DateTimeOffset.MinValue), Times.Once);
        Assert.AreEqual(1, h.ViewModel.Notifications.Count);
        Assert.AreEqual("same id on B", h.ViewModel.Notifications[0].Content);
    }

    [TestMethod]
    public async Task SendNotAvailableForBlankText()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.ViewModel.MessageText = "   ";

        Assert.IsFalse(h.ViewModel.SendCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task SendSendsTrimmedTextAndClearsInput()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.ViewModel.MessageText = "  hello world  ";
        Assert.IsTrue(h.ViewModel.SendCommand.CanExecute(null));

        await h.ViewModel.SendCommand.ExecuteAsync();

        h.Current.Verify(m => m.SendMessageAsync("hello world"), Times.Once);
        Assert.AreEqual(string.Empty, h.ViewModel.MessageText);
        Assert.IsFalse(h.ViewModel.HasError);
    }

    [TestMethod]
    public async Task SyncAddsMissedMessagesNewestOnTopWithoutAlerts()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        // unordered on purpose
        h.Current.SyncReturns(
            TestData.Msg("second", TestData.T0.AddMinutes(2)),
            TestData.Msg("first", TestData.T0.AddMinutes(1)),
            TestData.Msg("third", TestData.T0.AddMinutes(3)));
        List<NotificationItem> alerts = new();
        h.ViewModel.NotificationReceived += alerts.Add;

        await h.ViewModel.SyncCommand.ExecuteAsync();

        CollectionAssert.AreEqual(
            new[] { "third", "second", "first" },
            h.ViewModel.Notifications.Select(n => n.Content).ToArray());
        Assert.AreEqual(0, alerts.Count);
        StringAssert.EndsWith(h.ViewModel.LastSyncText, "· 3 new");
        StringAssert.StartsWith(h.ViewModel.LastSyncText, "Last synced");
    }

    [TestMethod]
    public async Task SyncFirstSyncStartsFromMinValueThenFromNewestMessage()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.SyncReturns(
            TestData.Msg("a", TestData.T0.AddMinutes(1)),
            TestData.Msg("b", TestData.T0.AddMinutes(9)));

        await h.ViewModel.SyncCommand.ExecuteAsync();
        await h.ViewModel.SyncCommand.ExecuteAsync();

        // the first (empty) sync happens while connecting
        h.Current.Verify(m => m.SyncAsync(DateTimeOffset.MinValue), Times.Exactly(2));
        h.Current.Verify(m => m.SyncAsync(TestData.T0.AddMinutes(9)), Times.Once);
    }

    [TestMethod]
    public async Task SyncSkipsMessagesAlreadyShown()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        Message live = TestData.Msg("live", TestData.T0);
        h.Current.RaiseMessage(live);
        h.Current.SyncReturns(live, TestData.Msg("missed", TestData.T0.AddMinutes(1)));

        await h.ViewModel.SyncCommand.ExecuteAsync();

        Assert.AreEqual(2, h.ViewModel.Notifications.Count);
        StringAssert.EndsWith(h.ViewModel.LastSyncText, "· 1 new");
    }

    [TestMethod]
    public async Task MessageReceivedAddsItemOnTopAndRaisesNotificationReceived()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        List<NotificationItem> alerts = new();
        h.ViewModel.NotificationReceived += alerts.Add;
        Message first = TestData.Msg("one", TestData.T0, sender: "alice");
        Message second = TestData.Msg("two", TestData.T0.AddMinutes(1), sender: "bob");

        h.Current.RaiseMessage(first);
        h.Current.RaiseMessage(second);

        Assert.AreEqual(2, h.ViewModel.Notifications.Count);
        Assert.AreEqual("two", h.ViewModel.Notifications[0].Content);
        Assert.AreEqual("bob", h.ViewModel.Notifications[0].SenderId);
        Assert.AreEqual(second.Id, h.ViewModel.Notifications[0].Id);
        Assert.AreEqual(second.Timestamp, h.ViewModel.Notifications[0].ReceivedAt);
        Assert.AreEqual(2, alerts.Count);
        Assert.AreSame(h.ViewModel.Notifications[0], alerts[1]);
    }

    [TestMethod]
    public async Task MessageReceivedDuplicateIsIgnored()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        int alerts = 0;
        h.ViewModel.NotificationReceived += _ => alerts++;
        Message m = TestData.Msg();

        h.Current.RaiseMessage(m);
        h.Current.RaiseMessage(m);

        Assert.AreEqual(1, h.ViewModel.Notifications.Count);
        Assert.AreEqual(1, alerts);
    }

    [TestMethod]
    public async Task NotificationsChangeRaisesIsEmptyAndUpdatesClearAvailability()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        List<string?> names = new();
        h.ViewModel.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        int canExecuteChanged = 0;
        h.ViewModel.ClearCommand.CanExecuteChanged += (_, _) => canExecuteChanged++;

        h.Current.RaiseMessage(TestData.Msg());

        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.IsEmpty));
        Assert.AreEqual(1, canExecuteChanged);
        Assert.IsFalse(h.ViewModel.IsEmpty);
        Assert.IsTrue(h.ViewModel.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ClearEmptiesList()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.RaiseMessage(TestData.Msg("a"));
        h.Current.RaiseMessage(TestData.Msg("b"));

        h.ViewModel.ClearCommand.Execute(null);

        Assert.IsTrue(h.ViewModel.IsEmpty);
        Assert.IsFalse(h.ViewModel.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ReconnectingUpdatesState()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.ViewModel.MessageText = "hi";

        h.Current.SetState(ConnectionState.Reconnecting);
        h.Current.RaiseReconnecting(new IOException("lost"));

        Assert.AreEqual(ConnectionState.Reconnecting, h.ViewModel.State);
        Assert.AreEqual("Reconnecting…", h.ViewModel.StateText);
        Assert.AreEqual("Connection lost. Trying to reconnect…", h.ViewModel.StateHint);
        Assert.IsFalse(h.ViewModel.ConnectCommand.CanExecute(null), "still on the same server");
        Assert.IsFalse(h.ViewModel.SendCommand.CanExecute(null), "cannot send while reconnecting");
        Assert.IsTrue(h.ViewModel.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ReconnectedClearsErrorAndUpdatesState()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.Current.SetState(ConnectionState.Disconnected);
        h.Current.RaiseClosed(new IOException("boom"));
        Assert.IsTrue(h.ViewModel.HasError);

        h.Current.SetState(ConnectionState.Connected);
        h.Current.RaiseReconnected("id");

        Assert.IsNull(h.ViewModel.ErrorText);
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
        // one sync while connecting, one after the connection came back by itself
        h.Current.Verify(m => m.SyncAsync(It.IsAny<DateTimeOffset>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task ClosedWithExceptionShowsErrorAndDisconnects()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.Current.SetState(ConnectionState.Disconnected);
        h.Current.RaiseClosed(new IOException("boom"));

        Assert.AreEqual("Connection closed: boom", h.ViewModel.ErrorText);
        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
        Assert.IsTrue(h.ViewModel.ConnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ClosedWithoutExceptionDisconnectsWithoutError()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.Current.SetState(ConnectionState.Disconnected);
        h.Current.RaiseClosed(null);

        Assert.IsFalse(h.ViewModel.HasError);
        Assert.AreEqual(ConnectionState.Disconnected, h.ViewModel.State);
    }

    [TestMethod]
    public async Task DisposeAsyncDisposesMessenger()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        await h.ViewModel.DisposeAsync();

        h.Current.Verify(m => m.DisposeAsync(), Times.Once);
    }


    [TestMethod]
    public void SelectingPresetFillsUrlBoxAndRaisesPropertyChanged()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;
        List<string?> names = new();
        vm.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        vm.SelectedPreset = vm.ServerPresets[0];

        Assert.AreEqual(vm.ServerPresets[0].Url, vm.ServerUrl);
        Assert.AreSame(vm.ServerPresets[0], vm.SelectedPreset);
        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.SelectedPreset));
        CollectionAssert.Contains(names, nameof(NotificationServiceViewModel.ServerUrl));
    }

    [TestMethod]
    public void TypingCustomUrlClearsPresetAndTypingPresetUrlSelectsIt()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;

        vm.ServerUrl = ViewModelHarness.Url;
        Assert.IsNull(vm.SelectedPreset);

        vm.ServerUrl = vm.ServerPresets[0].Url;
        Assert.AreSame(vm.ServerPresets[0], vm.SelectedPreset);
    }

    [TestMethod]
    public void ConnectNotAvailableForBlankUrl()
    {
        NotificationServiceViewModel vm = new ViewModelHarness().ViewModel;

        vm.ServerUrl = "   ";

        Assert.IsFalse(vm.ConnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ConnectWhenSyncFailsShowsErrorButStaysConnected()
    {
        NotificationServiceViewModel vm = new(url => {
            Mock<IMessenger> mock = TestMessenger.Create();
            mock.Setup(m => m.SyncAsync(It.IsAny<DateTimeOffset>())).ThrowsAsync(new IOException("sync failed"));
            return mock.Object;
        }, ViewModelHarness.RunNow);

        await vm.ConnectCommand.ExecuteAsync();

        Assert.AreEqual("sync failed", vm.ErrorText);
        Assert.AreEqual(ConnectionState.Connected, vm.State);
        Assert.IsFalse(vm.ConnectCommand.CanExecute(null), "the connection itself worked");
        Assert.IsTrue(vm.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ConnectNotAvailableWhenConnectedToTheTypedServer()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.ViewModel.ServerUrl = "  " + ViewModelHarness.Url + "  ";

        Assert.IsFalse(h.ViewModel.ConnectCommand.CanExecute(null));
        Assert.IsTrue(h.ViewModel.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ConnectNotAvailableWhileStateIsConnecting()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();
        h.ViewModel.ServerUrl = "https://b.example/hub";

        h.Current.SetState(ConnectionState.Connecting);
        h.Current.RaiseReconnecting();

        Assert.AreEqual(ConnectionState.Connecting, h.ViewModel.State);
        Assert.IsFalse(h.ViewModel.ConnectCommand.CanExecute(null));
        Assert.IsFalse(h.ViewModel.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ConnectWhileConnectedSwitchesServerAndClearsOldNotifications()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync("https://a.example/hub");
        h.Current.RaiseMessage(TestData.Msg("from A"));

        await h.ConnectAsync("https://b.example/hub");

        Assert.AreEqual(2, h.Messengers.Count);
        h.Messengers[0].Verify(m => m.DisposeAsync(), Times.Once);
        h.Messengers[1].Verify(m => m.ConnectAsync(), Times.Once);
        Assert.IsTrue(h.ViewModel.IsEmpty);
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
        Assert.IsFalse(h.ViewModel.ConnectCommand.CanExecute(null), "now on the typed server");
        Assert.IsTrue(h.ViewModel.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task SwitchingToServerThatFailsEndsDisconnectedAndCanRetry()
    {
        int created = 0;
        NotificationServiceViewModel vm = new(url => (++created == 1
            ? TestMessenger.Create()
            : TestMessenger.Create(connectError: new IOException("refused"))).Object, ViewModelHarness.RunNow) {
            ServerUrl = "https://a.example/hub"
        };
        await vm.ConnectCommand.ExecuteAsync();

        vm.ServerUrl = "https://b.example/hub";
        await vm.ConnectCommand.ExecuteAsync();

        Assert.AreEqual("refused", vm.ErrorText);
        Assert.AreEqual(ConnectionState.Disconnected, vm.State);
        Assert.IsTrue(vm.ConnectCommand.CanExecute(null));
        Assert.IsFalse(vm.DisconnectCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ConnectWithInvalidUrlWhileConnectedKeepsCurrentConnection()
    {
        ViewModelHarness h = new();
        await h.ConnectAsync();

        h.ViewModel.ServerUrl = "ftp://nope";
        await h.ViewModel.ConnectCommand.ExecuteAsync();

        StringAssert.Contains(h.ViewModel.ErrorText, "http://");
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
        Assert.AreEqual(1, h.Messengers.Count);
        h.Current.Verify(m => m.DisconnectAsync(), Times.Never);
    }


    [TestMethod]
    public void ConstructorWithoutUiDispatcherThrows()
    {
        // No runOnUi given: the view model looks for the current thread's DispatcherQueue.
        // A test worker thread has none (or the WinUI runtime is not running), so construction must fail.
        Exception? error = null;
        try
        {
            _ = new NotificationServiceViewModel(url => TestMessenger.Create().Object);
        }
        catch (Exception ex)
        {
            error = ex;
        }

        Assert.IsNotNull(error);
    }
}
