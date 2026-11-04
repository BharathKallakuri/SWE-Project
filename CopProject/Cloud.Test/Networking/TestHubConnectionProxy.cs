// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cloud.Networking;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Test;

[TestClass]
public class TestHubConnectionProxy
{
    private static HubConnectionProxy CreateProxy()
    {
        return new(new HubConnectionBuilder().WithUrl("http://localhost/hub").Build());
    }

    [TestMethod]
    public async Task StateIsDisconnectedInitially()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        Assert.AreEqual(HubConnectionState.Disconnected, proxy.State);
    }

    [TestMethod]
    public async Task EventsCanBeSubscribedAndUnsubscribed()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        Func<Exception?, Task> exceptionHandler = _ => Task.CompletedTask;
        Func<string?, Task> idHandler = _ => Task.CompletedTask;

        proxy.Reconnecting += exceptionHandler;
        proxy.Reconnecting -= exceptionHandler;
        proxy.Reconnected += idHandler;
        proxy.Reconnected -= idHandler;
        proxy.Closed += exceptionHandler;
        proxy.Closed -= exceptionHandler;
    }

    [TestMethod]
    public async Task OnReturnsADisposable()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        IDisposable dis = proxy.On<Message>(Contract.ReceiveMessage, _ => { });
        Assert.IsNotNull(dis);
        dis.Dispose();
    }

    [TestMethod]
    public async Task StartAsyncOnCancellingTheTokenThrows()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        using CancellationTokenSource cts = new();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => proxy.StartAsync(cts.Token));
    }

    [TestMethod]
    public async Task StopAsyncOnDisconnectedStateCompletes()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        await proxy.StopAsync();
        Assert.AreEqual(HubConnectionState.Disconnected, proxy.State);
    }

    [TestMethod]
    public async Task DisposeAsyncOnDisconnectedStateCompletes()
    {
        HubConnectionProxy proxy = CreateProxy();
        await proxy.DisposeAsync();
        Assert.AreEqual(HubConnectionState.Disconnected, proxy.State);
    }

    [TestMethod]
    public async Task InvokeAsyncOnDisconnectedStateThrows()
    {
        await using HubConnectionProxy proxy = CreateProxy();
        await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.InvokeAsync(Contract.SendMessage, "Hello"));
    }
}
