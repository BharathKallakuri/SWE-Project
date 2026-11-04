// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;
using Microsoft.AspNetCore.SignalR.Client;
using Moq;

namespace Cloud.Test;

[TestClass]
public class TestSignalRMessenger
{
    private Mock<IHubConnection> _hub = null!;

    /* Simulates the server pushing a message: we capture the handler that the
     * messenger registered with On<Message> and call it ourselves. */
    private Action<Message>? _onMessage;

    [TestInitialize]
    public void Init()
    {
        _hub = new Mock<IHubConnection>();

        _hub.Setup(h => h.On(Contract.ReceiveMessage, It.IsAny<Action<Message>>()))
            .Callback<string, Action<Message>>((_, handler) => _onMessage = handler)
            .Returns(Mock.Of<IDisposable>());

        _hub.SetupGet(h => h.State).Returns(HubConnectionState.Disconnected);
    }

    private SignalRMessenger CreateMessenger()
    {
        return new(_hub.Object);
    }

    private static Message NewMessage(string content = "hello")
    {
        return new() { SenderId = "sender-123", Content = content };
    }

    /* constructors */
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void PublicConstructorRejectsBlankUrl(string url)
    {
        Assert.Throws<ArgumentException>(() => new SignalRMessenger(url));
    }

    [TestMethod]
    public void PublicConstructorRejectsNullUrl()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRMessenger((string)null!));
    }

    [TestMethod]
    public void InternalConstructorRejectsNullConnection()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRMessenger((IHubConnection)null!));
    }

    [TestMethod]
    public async Task PublicConstructorWithValidUrlIsInDisconnectedState()
    {
        await using SignalRMessenger messenger = new("http://localhost/hub");
        Assert.AreEqual(HubConnectionState.Disconnected, messenger.State);
    }

    [TestMethod]
    public void ConstructorRegistersReceiveHandlerOnce()
    {
        CreateMessenger();
        _hub.Verify(h => h.On(Contract.ReceiveMessage, It.IsAny<Action<Message>>()), Times.Once);
    }

    /* state */
    [TestMethod]
    [DataRow(HubConnectionState.Disconnected)]
    [DataRow(HubConnectionState.Connecting)]
    [DataRow(HubConnectionState.Connected)]
    [DataRow(HubConnectionState.Reconnecting)]
    public void StateReflectsTheConnectionState(HubConnectionState state)
    {
        _hub.SetupGet(h => h.State).Returns(state);
        Assert.AreEqual(state, CreateMessenger().State);
    }

    /* message recieved */
    [TestMethod]
    public void IncomingMessageRaisesMessageReceived()
    {
        SignalRMessenger messenger = CreateMessenger();
        Message? received = null;
        messenger.MessageReceived += m => received = m;
        Message sent = NewMessage();
        _onMessage!(sent);
        Assert.AreSame(sent, received);
    }

    [TestMethod]
    public void IncomingMessageReachesEverySubscriber()
    {
        SignalRMessenger messenger = CreateMessenger();
        int calls = 0;
        messenger.MessageReceived += _ => calls++;
        messenger.MessageReceived += _ => calls++;
        _onMessage!(NewMessage());
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public void IncomingMessageDoesNotReachAnUnsubscriber()
    {
        SignalRMessenger messenger = CreateMessenger();
        int calls = 0;
        Action<Message> handler = _ => calls++;
        messenger.MessageReceived += handler;
        _onMessage!(NewMessage());
        Assert.AreEqual(1, calls);

        messenger.MessageReceived -= handler;
        _onMessage!(NewMessage());
        Assert.AreEqual(1, calls);
    }

    /* events */
    [TestMethod]
    public void ReconnectingIsForwarded()
    {
        SignalRMessenger messenger = CreateMessenger();
        Exception? received = null;
        messenger.Reconnecting += e => received = e;
        InvalidOperationException error = new();
        _hub.Raise(h => h.Reconnecting += null, error);
        Assert.AreSame(error, received);
    }

    [TestMethod]
    public void ReconnectedIsForwarded()
    {
        SignalRMessenger messenger = CreateMessenger();
        string? received = null;
        messenger.Reconnected += id => received = id;
        _hub.Raise(h => h.Reconnected += null, "new-id");
        Assert.AreEqual("new-id", received);
    }

    [TestMethod]
    public void ClosedIsForwarded()
    {
        SignalRMessenger messenger = CreateMessenger();
        Exception? received = null;
        messenger.Closed += e => received = e;
        InvalidOperationException error = new();
        _hub.Raise(h => h.Closed += null, error);
        Assert.AreSame(error, received);
    }

    /* ConnectAsync*/
    [TestMethod]
    public async Task ConnectAsyncStartsTheConnection()
    {
        await CreateMessenger().ConnectAsync();
        _hub.Verify(h => h.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ConnectAsyncDoesNothingWhenAlreadyConnected()
    {
        _hub.SetupGet(h => h.State).Returns(HubConnectionState.Connected);
        await CreateMessenger().ConnectAsync();
        _hub.Verify(h => h.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [DataRow(HubConnectionState.Connecting)]
    [DataRow(HubConnectionState.Reconnecting)]
    public async Task ConnectAsyncWhenNotInDisconnectedStateThrows(HubConnectionState state)
    {
        _hub.SetupGet(h => h.State).Returns(state);
        await Assert.ThrowsAsync<InvalidOperationException>(CreateMessenger().ConnectAsync);
        _hub.Verify(h => h.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ConnectAsyncRetriesOnce()
    {
        _hub.SetupSequence(h => h.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException())
            .Returns(Task.CompletedTask);

        await CreateMessenger().ConnectAsync();
        _hub.Verify(h => h.StartAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task ConnectAsyncAfterThreeAttemptsThrows()
    {
        HttpRequestException last = new("third");
        _hub.SetupSequence(h => h.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("first"))
            .ThrowsAsync(new HttpRequestException("second"))
            .ThrowsAsync(last);

        HttpRequestException thrown = await Assert.ThrowsAsync<HttpRequestException>(CreateMessenger().ConnectAsync);
        Assert.AreSame(last, thrown);
        _hub.Verify(h => h.StartAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    /*SendMessageAsync*/
    [TestMethod]
    public async Task SendMessageAsyncInvokesProperly()
    {
        await CreateMessenger().SendMessageAsync("Hello");
        _hub.Verify(h => h.InvokeAsync(Contract.SendMessage, "Hello", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task SendMessageAsyncRejectsBlankContent(string content)
    {
        SignalRMessenger messenger = CreateMessenger();
        await Assert.ThrowsAsync<ArgumentException>(() => messenger.SendMessageAsync(content));
        _hub.Verify(h => h.InvokeAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SendMessageAsyncRejectsNullContent()
    {
        SignalRMessenger messenger = CreateMessenger();
        await Assert.ThrowsAsync<ArgumentNullException>(() => messenger.SendMessageAsync(null!));
        _hub.Verify(h => h.InvokeAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SendMessageAsyncPropagatesHubFailure()
    {
        InvalidOperationException error = new("not connected");
        _hub.Setup(h => h.InvokeAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);
        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateMessenger().SendMessageAsync("Hello"));
        Assert.AreSame(error, thrown);
    }

    /*DisconnectAsync*/
    [TestMethod]
    public async Task DisconnectAsyncStopsTheConnection()
    {
        await CreateMessenger().DisconnectAsync();
        _hub.Verify(h => h.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task DisconnectAsyncDoesNotDisposeTheConnection()
    {
        await CreateMessenger().DisconnectAsync();
        _hub.Verify(h => h.DisposeAsync(), Times.Never);
    }

    /*DisposeAsync*/
    [TestMethod]
    public async Task DisposeAsyncDisposesTheConnection()
    {
        await CreateMessenger().DisposeAsync();
        _hub.Verify(h => h.DisposeAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DisposeAsyncDoesNotStopTheConnectionSeparately()
    {
        await CreateMessenger().DisposeAsync();
        _hub.Verify(h => h.StopAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
