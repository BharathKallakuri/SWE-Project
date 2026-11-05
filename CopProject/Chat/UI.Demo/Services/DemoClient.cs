using System.Net;
using Microsoft.UI.Dispatching;

namespace UI.Demo.Services;

/// <summary>
/// Connects UI demo participant sessions to a remote ChatServer.
/// </summary>
public sealed class DemoClient : IDisposable
{
    private readonly IPEndPoint _serverEndpoint;
    private readonly string _roomName;
    private bool _disposed;

    public DemoClient(string roomName, IPEndPoint serverEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomName);
        ArgumentNullException.ThrowIfNull(serverEndpoint);
        _roomName = roomName;
        _serverEndpoint = serverEndpoint;
    }

    public IPEndPoint ServerEndpoint => _serverEndpoint;

    public DemoParticipantSession CreateParticipant(
        string username,
        DispatcherQueue dispatcherQueue)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return DemoParticipantSession.Create(
            username,
            _roomName,
            "Officer 1",
            _serverEndpoint,
            dispatcherQueue);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
