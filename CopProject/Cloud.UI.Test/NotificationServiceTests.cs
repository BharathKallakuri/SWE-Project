// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.UI.Services;

namespace Cloud.UI.Test;

/// <summary>
/// Tests the service that owns the messenger and forwards its events.
/// </summary>
[TestClass]
public class NotificationServiceTests
{
    private const string UrlA = "https://a.example/hub";
    private const string UrlB = "https://b.example/hub";

    private readonly List<Mock<IMessenger>> _created = new();
    private readonly List<string> _urls = new();

    private NotificationService CreateService()
    {
        return new NotificationService(url => {
            Mock<IMessenger> mock = TestMessenger.Create();
            _created.Add(mock);
            _urls.Add(url);
            return mock.Object;
        });
    }

    /// <summary>Valid http/https URLs are accepted and normalised.</summary>
    [TestMethod]
    [DataRow("https://localhost:5001/hub", "https://localhost:5001/hub")]
    [DataRow("http://example.com", "http://example.com/")]
    [DataRow("  https://example.com/x  ", "https://example.com/x")]
    public void TryParseUrlValidUrlReturnsNormalisedForm(string input, string expected)
    {
        Assert.IsTrue(NotificationService.TryParseUrl(input, out string url));
        Assert.AreEqual(expected, url);
    }

    /// <summary>Anything that is not an absolute http/https URL is rejected.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("ftp://example.com")]
    [DataRow("not a url")]
    public void TryParseUrlInvalidUrlReturnsFalseAndEmpty(string input)
    {
        Assert.IsFalse(NotificationService.TryParseUrl(input, out string url));
        Assert.AreEqual(string.Empty, url);
    }

    /// <summary>A null messenger factory is rejected.</summary>
    [TestMethod]
    public void ConstructorNullFactoryThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new NotificationService(null!));
    }

    /// <summary>Without a messenger the state is Disconnected.</summary>
    [TestMethod]
    public void StateWithoutMessengerIsDisconnected()
    {
        Assert.AreEqual(ConnectionState.Disconnected, CreateService().State);
    }

    /// <summary>Connecting before a server is chosen fails.</summary>
    [TestMethod]
    public async Task ConnectAsyncBeforeUseServerThrows()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => CreateService().ConnectAsync());
    }

    /// <summary>Disconnecting without a messenger is a no-op.</summary>
    [TestMethod]
    public async Task DisconnectAsyncWithoutMessengerDoesNothing()
    {
        await CreateService().DisconnectAsync();
    }

    /// <summary>The first call creates a messenger and raises ServerChanged.</summary>
    [TestMethod]
    public async Task UseServerAsyncFirstCallCreatesMessengerAndRaisesServerChanged()
    {
        NotificationService service = CreateService();
        int serverChanged = 0;
        service.ServerChanged += () => serverChanged++;

        await service.UseServerAsync(UrlA);

        Assert.AreEqual(1, _created.Count);
        Assert.AreEqual(UrlA, _urls[0]);
        Assert.AreEqual(1, serverChanged);
        _created[0].Verify(m => m.DisconnectAsync(), Times.Never);
    }

    /// <summary>The same URL reuses the messenger.</summary>
    [TestMethod]
    public async Task UseServerAsyncSameUrlReusesMessenger()
    {
        NotificationService service = CreateService();
        int serverChanged = 0;
        service.ServerChanged += () => serverChanged++;

        await service.UseServerAsync(UrlA);
        await service.UseServerAsync(UrlA);

        Assert.AreEqual(1, _created.Count);
        Assert.AreEqual(1, serverChanged);
        _created[0].Verify(m => m.DisposeAsync(), Times.Never);
    }

    /// <summary>The same URL while connected closes the connection but keeps the messenger.</summary>
    [TestMethod]
    public async Task UseServerAsyncSameUrlWhileConnectedDisconnects()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);
        await service.ConnectAsync();

        await service.UseServerAsync(UrlA);

        _created[0].Verify(m => m.DisconnectAsync(), Times.Once);
        Assert.AreEqual(ConnectionState.Disconnected, service.State);
    }

    /// <summary>A different URL disposes the old messenger and creates a new one.</summary>
    [TestMethod]
    public async Task UseServerAsyncDifferentUrlDisposesOldAndCreatesNew()
    {
        NotificationService service = CreateService();
        int serverChanged = 0;
        service.ServerChanged += () => serverChanged++;

        await service.UseServerAsync(UrlA);
        await service.UseServerAsync(UrlB);

        Assert.AreEqual(2, _created.Count);
        Assert.AreEqual(UrlB, _urls[1]);
        Assert.AreEqual(2, serverChanged);
        _created[0].Verify(m => m.DisposeAsync(), Times.Once);
        _created[1].Verify(m => m.DisposeAsync(), Times.Never);
    }

    /// <summary>Events of the replaced messenger are no longer forwarded.</summary>
    [TestMethod]
    public async Task UseServerAsyncDifferentUrlUnsubscribesFromOldMessenger()
    {
        NotificationService service = CreateService();
        int received = 0;
        service.MessageReceived += _ => received++;
        await service.UseServerAsync(UrlA);
        Mock<IMessenger> old = _created[0];

        await service.UseServerAsync(UrlB);
        old.RaiseMessage(TestData.Msg());

        old.VerifyRemove(m => m.MessageReceived -= It.IsAny<Action<Message>>(), Times.Once);
        Assert.AreEqual(0, received);
    }

    /// <summary>A failing dispose of the old messenger is ignored.</summary>
    [TestMethod]
    public async Task UseServerAsyncOldMessengerDisposeThrowsIsSwallowed()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);
        _created[0].Setup(m => m.DisposeAsync()).Throws(new InvalidOperationException("nope"));

        await service.UseServerAsync(UrlB);

        Assert.AreEqual(2, _created.Count);
    }

    /// <summary>Connect and disconnect are passed on to the messenger.</summary>
    [TestMethod]
    public async Task ConnectAsyncAndDisconnectAsyncDriveTheMessenger()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);

        await service.ConnectAsync();
        Assert.AreEqual(ConnectionState.Connected, service.State);
        await service.DisconnectAsync();

        _created[0].Verify(m => m.ConnectAsync(), Times.Once);
        _created[0].Verify(m => m.DisconnectAsync(), Times.Once);
        Assert.AreEqual(ConnectionState.Disconnected, service.State);
    }

    /// <summary>The text is passed on to the messenger.</summary>
    [TestMethod]
    public async Task SendAsyncForwardsText()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);

        await service.SendAsync("ping");

        _created[0].Verify(m => m.SendMessageAsync("ping"), Times.Once);
    }

    /// <summary>The since time is passed on and the missed messages are returned.</summary>
    [TestMethod]
    public async Task SyncAsyncForwardsSinceAndReturnsMessages()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);
        Message missed = TestData.Msg("missed");
        _created[0].SyncReturns(missed);

        IReadOnlyList<Message> result = await service.SyncAsync(TestData.T0);

        _created[0].Verify(m => m.SyncAsync(TestData.T0), Times.Once);
        Assert.AreEqual(missed.Id, result.Single().Id);
    }

    /// <summary>Messenger events are ignored without subscribers and forwarded with them.</summary>
    [TestMethod]
    public async Task MessengerEventsAreIgnoredWithoutSubscribersAndForwardedWithThem()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);
        Message sent = TestData.Msg();
        Exception e1 = new("r");
        Exception e2 = new("c");

        // nobody listens yet: must not throw
        _created[0].RaiseMessage(sent);
        _created[0].RaiseReconnecting(e1);
        _created[0].RaiseReconnected("conn-0");
        _created[0].RaiseClosed(e2);

        Message? message = null;
        Exception? reconnecting = null;
        string? reconnectedId = null;
        Exception? closed = null;
        service.MessageReceived += m => message = m;
        service.Reconnecting += e => reconnecting = e;
        service.Reconnected += id => reconnectedId = id;
        service.Closed += e => closed = e;

        _created[0].RaiseMessage(sent);
        _created[0].RaiseReconnecting(e1);
        _created[0].RaiseReconnected("conn-1");
        _created[0].RaiseClosed(e2);

        Assert.AreEqual(sent.Id, message?.Id);
        Assert.AreSame(e1, reconnecting);
        Assert.AreEqual("conn-1", reconnectedId);
        Assert.AreSame(e2, closed);
    }

    /// <summary>Disposing releases the messenger and the state becomes Disconnected.</summary>
    [TestMethod]
    public async Task DisposeAsyncDisposesMessengerAndResetsState()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);

        await service.DisposeAsync();

        _created[0].Verify(m => m.DisposeAsync(), Times.Once);
        Assert.AreEqual(ConnectionState.Disconnected, service.State);
    }

}
