// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using Cloud.Networking;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Test;

[TestClass]
public class TestSignalRMessenger
{
    // never contacted just some placeholder but valid url 
    private const string ValidUrl = "http://localhost/hub";

    /* 
     * Tests that need a live hub are not covered here 
     * The things i.e not covered identify that by just running this
     * for coverage analysis.
     * 
     * NOTE: await using will dispose off the connection so dont change it
     */

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("       ")]
    public void ConstructorRejectsEmptyUrl(string url)
    {
        Assert.Throws<ArgumentException>(() => new SignalRMessenger(url));
    }

    [TestMethod]
    public void ConstructorRejectsNullUrl()
    {
        Assert.Throws<ArgumentNullException>(() => new SignalRMessenger((string)null!));
        //Assert.Throws<ArgumentNullException>(() => new SignalRMessenger((IHubConnection)null!));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("       ")]
    public async Task SendMessageAsyncRejectsEmptyContent(string content)
    {
        await using SignalRMessenger messenger = new SignalRMessenger(ValidUrl);
        await Assert.ThrowsAsync<ArgumentException>(() => messenger.SendMessageAsync(content));
    }

    [TestMethod]
    public async Task SendMessageAsyncRejectsNullContent()
    {
        await using SignalRMessenger messenger = new SignalRMessenger(ValidUrl);
        await Assert.ThrowsAsync<ArgumentNullException>(() => messenger.SendMessageAsync(null!));
    }

    [TestMethod]
    public async Task MessengerStartsInDisconnectedState()
    {
        await using SignalRMessenger messenger = new SignalRMessenger(ValidUrl);
        Assert.AreEqual(HubConnectionState.Disconnected, messenger.State);
    }

    [TestMethod]
    public async Task SendMessageAsyncThrowsWhenInDisconnectedState()
    {
        await using SignalRMessenger messenger = new SignalRMessenger(ValidUrl);
        await Assert.ThrowsAsync<InvalidOperationException>(() => messenger.SendMessageAsync("Hello"));
    }

    [TestMethod]
    public async Task DisconnectAsyncWhenInDisconnectedStateDoesNothing()
    {
        await using SignalRMessenger messenger = new SignalRMessenger(ValidUrl);
        await messenger.DisconnectAsync();
        Assert.AreEqual(HubConnectionState.Disconnected, messenger.State);
    }

    [TestMethod]
    public async Task DisposeAsyncCanBeCalledAfterDisposing()
    {
        SignalRMessenger messenger = new SignalRMessenger(ValidUrl);

        await messenger.DisposeAsync();
        await messenger.DisposeAsync();
    }

    [TestMethod]
    public async Task AfterDisposeAsyncRestWillThrow()
    {
        SignalRMessenger messenger = new SignalRMessenger(ValidUrl);
        await messenger.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(messenger.ConnectAsync);
        await Assert.ThrowsAsync<ObjectDisposedException>(messenger.DisconnectAsync);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => messenger.SendMessageAsync("Hello"));
    }

    [TestMethod]
    public async Task ConnectAsyncRetriesThenThrowsWhenConnectionIsRefused()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        await using SignalRMessenger messenger = new SignalRMessenger($"http://127.0.0.1:{port}/hub");

        await Assert.ThrowsAsync<Exception>(messenger.ConnectAsync);
        Assert.AreEqual(HubConnectionState.Disconnected, messenger.State);
    }

    [TestMethod]
    public async Task ConnectAsyncThrowsWhenNotInConnectingState()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        // disconnected
        await using var messenger = new SignalRMessenger($"http://127.0.0.1:{port}/hub");

        Task firstConnection = messenger.ConnectAsync();
        // disconnected to connecting/reconnecting
        while (messenger.State != HubConnectionState.Connecting)
        {
            await Task.Delay(10);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(messenger.ConnectAsync);
    }
}
