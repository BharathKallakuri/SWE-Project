// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.UI.Services;

namespace Cloud.UI.Test;

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

    [TestMethod]
    [DataRow("https://localhost:5001/hub", "https://localhost:5001/hub")]
    [DataRow("http://example.com", "http://example.com/")]
    [DataRow("  https://example.com/x  ", "https://example.com/x")]
    [DataRow("HTTP://EXAMPLE.COM/", "http://example.com/")]
    public void TryParseUrlValidUrlReturnsNormalisedForm(string input, string expected)
    {
        Assert.IsTrue(NotificationService.TryParseUrl(input, out string url));
        Assert.AreEqual(expected, url);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("localhost:5001")]
    [DataRow("/relative/path")]
    [DataRow("ftp://example.com")]
    [DataRow("file:///c:/x")]
    [DataRow("not a url")]
    public void TryParseUrlInvalidUrlReturnsFalseAndEmpty(string input)
    {
        Assert.IsFalse(NotificationService.TryParseUrl(input, out string url));
        Assert.AreEqual(string.Empty, url);
    }

    [TestMethod]
    public void ConstructorNullFactoryThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new NotificationService(null!));
    }

    [TestMethod]
    public void StateWithoutMessengerIsDisconnected()
    {
        Assert.AreEqual(ConnectionState.Disconnected, CreateService().State);
    }

    [TestMethod]
    public async Task ConnectAsyncBeforeUseServerThrows()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => CreateService().ConnectAsync());
    }

    [TestMethod]
    public async Task DisconnectAsyncWithoutMessengerDoesNothing()
    {
        await CreateService().DisconnectAsync();
    }

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

    [TestMethod]
    public async Task UseServerAsyncOldMessengerDisposeThrowsIsSwallowed()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);
        _created[0].Setup(m => m.DisposeAsync()).Throws(new InvalidOperationException("nope"));

        await service.UseServerAsync(UrlB);

        Assert.AreEqual(2, _created.Count);
    }

    [TestMethod]
    public async Task ConnectAsyncConnectsMessengerAndStateFollows()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);

        await service.ConnectAsync();

        _created[0].Verify(m => m.ConnectAsync(), Times.Once);
        Assert.AreEqual(ConnectionState.Connected, service.State);
    }

    [TestMethod]
    public async Task DisconnectAsyncDisconnectsMessenger()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);
        await service.ConnectAsync();

        await service.DisconnectAsync();

        _created[0].Verify(m => m.DisconnectAsync(), Times.Once);
        Assert.AreEqual(ConnectionState.Disconnected, service.State);
    }

    [TestMethod]
    public async Task SendAsyncForwardsText()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);

        await service.SendAsync("ping");

        _created[0].Verify(m => m.SendMessageAsync("ping"), Times.Once);
    }

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

    [TestMethod]
    public async Task MessengerEventsAreForwarded()
    {
        NotificationService service = CreateService();
        Message? message = null;
        Exception? reconnecting = null;
        string? reconnectedId = null;
        Exception? closed = null;
        service.MessageReceived += m => message = m;
        service.Reconnecting += e => reconnecting = e;
        service.Reconnected += id => reconnectedId = id;
        service.Closed += e => closed = e;
        await service.UseServerAsync(UrlA);
        Message sent = TestData.Msg();
        Exception e1 = new("r");
        Exception e2 = new("c");

        _created[0].RaiseMessage(sent);
        _created[0].RaiseReconnecting(e1);
        _created[0].RaiseReconnected("conn-1");
        _created[0].RaiseClosed(e2);

        Assert.AreEqual(sent.Id, message?.Id);
        Assert.AreSame(e1, reconnecting);
        Assert.AreEqual("conn-1", reconnectedId);
        Assert.AreSame(e2, closed);
    }

    [TestMethod]
    public async Task DisposeAsyncDisposesMessengerAndResetsState()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);

        await service.DisposeAsync();

        _created[0].Verify(m => m.DisposeAsync(), Times.Once);
        Assert.AreEqual(ConnectionState.Disconnected, service.State);
    }

    [TestMethod]
    public async Task DisposeAsyncMessengerDisposeThrowsIsSwallowed()
    {
        NotificationService service = CreateService();
        await service.UseServerAsync(UrlA);
        _created[0].Setup(m => m.DisposeAsync()).Throws(new InvalidOperationException("nope"));

        await service.DisposeAsync();
    }
}
