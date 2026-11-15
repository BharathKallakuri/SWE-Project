// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using Cloud.Networking.Server.Models;
using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using Moq;

namespace Cloud.Networking.Server.Test;

/// <summary>
/// Tests the MonitorHub filter.
/// </summary>
/// <remarks>
/// Rationales on Mocking choices:
/// it is obvious on why we should mock server monitor
///     <see cref="HubLifetimeContext"/> 
///     contains <see cref="HubCallerContext"/>, <see cref="IServiceProvider"/>, <see cref="Hub"/>
/// as we have hub lets fix it and use the real one (its store is irrelevant here)
/// Rest two we can mock as one is abstract and other is an interface
/// so we will do that itself.
/// </remarks>
[TestClass]
public class TestMonitorHubFilter
{
    private Mock<IServerMonitor> _monitor = null!;
    private Mock<HubCallerContext> _caller = null!;
    private Mock<IServiceProvider> _services = null!;
    private MonitorHubFilter _filter = null!;
    private Hub _hub = null!; /* using the real hub is easier */

    /// <summary>Initializes the dependencies used by the tests.</summary>
    [TestInitialize]
    public void Init()
    {
        _monitor = new Mock<IServerMonitor>();
        _caller = new Mock<HubCallerContext>();
        _caller.SetupGet(c => c.ConnectionId).Returns("conn-1");
        _services = new Mock<IServiceProvider>();
        _hub = new MessageHub(new Mock<IMessageStore>().Object);

        _filter = new MonitorHubFilter(_monitor.Object);
    }

    private HubLifetimeContext CreateLifetime()
    {
        return new(
            _caller.Object,
            _services.Object,
            _hub
        );
    }

    /// <summary>Tests that a connection is registered after the connection pipeline succeeds.</summary>
    [TestMethod]
    public async Task OnConnectedRegistersConnection()
    {
        bool registered = false;

        _monitor.Setup(m => m.Connected("conn-1"))
            .Callback(() => registered = true);

        await _filter.OnConnectedAsync(
            CreateLifetime(),
            _ => {
                Assert.IsFalse(registered);
                return Task.CompletedTask;
            });

        Assert.IsTrue(registered);
        _monitor.Verify(m => m.Connected("conn-1"), Times.Once);
    }

    /// <summary>Tests that a connection is not registered when the connection pipeline throws.</summary>
    [TestMethod]
    public async Task OnConnectedDoesNotRegisterWhenNextThrows()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _filter.OnConnectedAsync(
                CreateLifetime(),
                _ => throw new InvalidOperationException("fail")));

        _monitor.Verify(m => m.Connected(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Tests that a connection is removed when the client disconnects.</summary>
    [TestMethod]
    public async Task OnDisconnectedRemovesConnection()
    {
        await _filter.OnDisconnectedAsync(
            CreateLifetime(),
            null,
            (_, _) => Task.CompletedTask);

        _monitor.Verify(m => m.Disconnected("conn-1"), Times.Once);
    }

    /// <summary>Tests that a connection is removed even when the disconnect pipeline throws.</summary>
    [TestMethod]
    public async Task OnDisconnectedRemovesConnectionWhenNextThrows()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _filter.OnDisconnectedAsync(
                CreateLifetime(),
                null,
                (_, _) => throw new InvalidOperationException("fail")));

        _monitor.Verify(m => m.Disconnected("conn-1"), Times.Once);
    }

    private HubInvocationContext CreateInvocation()
    {
        MethodInfo method = typeof(MessageHub).GetMethod(nameof(MessageHub.SendMessage))!;
        return new HubInvocationContext(_caller.Object, _services.Object, _hub, method, ["hello"]);
    }

    /// <summary>Tests that a disconnect carrying an exception still removes the connection.</summary>
    [TestMethod]
    public async Task OnDisconnectedWithExceptionRemovesConnection()
    {
        await _filter.OnDisconnectedAsync(
            CreateLifetime(),
            new InvalidOperationException("dropped"),
            (_, _) => Task.CompletedTask);

        _monitor.Verify(m => m.Disconnected("conn-1"), Times.Once);
    }

    /// <summary>Tests that a successful hub call returns the result of the hub method.</summary>
    [TestMethod]
    public async Task InvokeMethodReturnsTheResultOfNext()
    {
        object expected = new();

        object? result = await _filter.InvokeMethodAsync(CreateInvocation(), _ => new ValueTask<object?>(expected));

        Assert.AreSame(expected, result);
    }

    /// <summary>Tests that a HubException is rethrown.</summary>
    [TestMethod]
    public async Task InvokeMethodRethrowsHubException()
    {
        await Assert.ThrowsAsync<HubException>(
            async () => await _filter.InvokeMethodAsync(CreateInvocation(), _ => throw new HubException("bad input")));
    }

    /// <summary>Tests that any other exception is rethrown.</summary>
    [TestMethod]
    public async Task InvokeMethodRethrowsOtherExceptions()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _filter.InvokeMethodAsync(CreateInvocation(), _ => throw new InvalidOperationException("bug")));
    }
}
