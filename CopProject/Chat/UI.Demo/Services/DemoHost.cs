using System.Diagnostics;
using System.Net;
using Chat;
using Microsoft.UI.Dispatching;
using Networking.TCPCommunication;
using UI.Services;
using UI.ViewModels;

namespace UI.Demo.Services;

/// <summary>
/// Owns one authoritative ChatServer and creates independent participant sessions.
/// Uses only the existing public Networking API; this project does not modify Networking.
/// </summary>
public sealed class DemoHost : IDisposable
{
    private readonly CommunicatorServer _serverCommunicator;
    private readonly MockChatController _controller;
    private readonly ChatServer _chatServer;
    private readonly string _roomName;
    private bool _disposed;

    public DemoHost(
        string roomName,
        string hostUsername,
        IPAddress bindAddress,
        int serverPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostUsername);
        ArgumentNullException.ThrowIfNull(bindAddress);
        if (serverPort is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(serverPort));

        _roomName = roomName;

        // This is the existing Networking API. The actual bind/listen behavior
        // remains owned by the Networking team.
        _serverCommunicator = new CommunicatorServer(
            new IPEndPoint(bindAddress, serverPort));
        if (_serverCommunicator.ListenPort <= 0)
        {
            _serverCommunicator.Dispose();
            throw new InvalidOperationException(
                "The demo TCP server could not start. Check the existing Networking implementation and port availability.");
        }

        ServerEndpoint = new IPEndPoint(
            _serverCommunicator.ListenIpAddress,
            _serverCommunicator.ListenPort);
        ClientEndpoint = bindAddress.Equals(IPAddress.Any)
            ? new IPEndPoint(IPAddress.Loopback, serverPort)
            : bindAddress.Equals(IPAddress.IPv6Any)
                ? new IPEndPoint(IPAddress.IPv6Loopback, serverPort)
                : ServerEndpoint;

        _controller = new MockChatController(hostUsername, _serverCommunicator);

        try
        {
            // Officer 1 is the demo host. Every local room uses the same host
            // identity so host-only actions are shown and authorized consistently.
            Chatroom room = new(roomName, hostUsername);

            _chatServer = new ChatServer(room, _serverCommunicator, _controller);
            _chatServer.DeliveryFailed += username =>
                Trace.WriteLine($"[Chat UI Demo] ChatServer could not queue an update for '{username}'.");

            Trace.WriteLine($"[Chat UI Demo] ChatServer created at {ServerEndpoint}.");
        }
        catch
        {
            _controller.Dispose();
            _serverCommunicator.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Network endpoint advertised by the existing CommunicatorServer.
    /// </summary>
    public IPEndPoint ServerEndpoint { get; }

    /// <summary>
    /// Endpoint used by participant windows on this machine.
    /// </summary>
    public IPEndPoint ClientEndpoint { get; }

    /// <summary>
    /// Creates one independent TCP client, ChatClient, adapter, local room and ViewModel.
    /// Must be called from the UI thread.
    /// </summary>
    public DemoParticipantSession CreateParticipant(
        string username,
        DispatcherQueue dispatcherQueue)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        DemoParticipantSession session = DemoParticipantSession.Create(
            username,
            _roomName,
            _controller.HostName,
            ClientEndpoint,
            dispatcherQueue);

        Trace.WriteLine(
            $"[Chat UI Demo] Participant '{username}' joined the meeting. " +
            $"Room participants: {string.Join(", ", _chatServer.Chatroom.Participants.Order(StringComparer.Ordinal))}.");

        return session;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _chatServer.Dispose();
        _controller.Dispose();
        _serverCommunicator.Dispose();
        Trace.WriteLine("[Chat UI Demo] Shared ChatServer stopped.");
    }
}
