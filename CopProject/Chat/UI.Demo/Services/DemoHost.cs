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
    private const string HostUsername = "Officer 1";
    private bool _disposed;

    public DemoHost(string roomName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomName);
        _roomName = roomName;

        // This is the existing Networking API. The actual bind/listen behavior
        // remains owned by the Networking team.
        _serverCommunicator = new CommunicatorServer(null);
        if (_serverCommunicator.ListenPort <= 0)
        {
            _serverCommunicator.Dispose();
            throw new InvalidOperationException(
                "The demo TCP server could not start. Check the existing Networking implementation and port availability.");
        }

        ServerEndpoint = new IPEndPoint(
            _serverCommunicator.ListenIpAddress,
            _serverCommunicator.ListenPort);

        _controller = new MockChatController(HostUsername, _serverCommunicator);

        try
        {
            // Officer 1 is the demo host. Every local room uses the same host
            // identity so host-only actions are shown and authorized consistently.
            Chatroom room = new(roomName, HostUsername);

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

        // CommunicatorClient connects synchronously in the existing Networking API.
        var communicator = new CommunicatorClient(ServerEndpoint);
        if (communicator.ListenPort <= 0)
        {
            communicator.Dispose();
            throw new InvalidOperationException(
                $"Participant '{username}' could not connect to the demo server at {ServerEndpoint}.");
        }

        ChatClient? client = null;
        MockChatClientController? participantController = null;
        ChatClientAdapter? adapter = null;
        ChatPanelViewModel? viewModel = null;

        try
        {
            // Subscribe the ChatClient before registering the participant.
            // MainWindow initializes the ViewModel and issues Sync after creation,
            // so the client also requests a fresh snapshot after the TCP accept completes.
            Chatroom localRoom = new(_roomName, _controller.HostName);
            client = new ChatClient(username, localRoom, communicator, ServerEndpoint);
            participantController = new MockChatClientController(
                username,
                communicator,
                ServerEndpoint);
            adapter = new ChatClientAdapter(client, dispatcherQueue);
            viewModel = new ChatPanelViewModel(adapter);

            participantController.JoinMeeting();

            Trace.WriteLine(
                $"[Chat UI Demo] Participant '{username}' joined the meeting. " +
                $"Room participants: {string.Join(", ", _chatServer.Chatroom.Participants.Order(StringComparer.Ordinal))}.");

            return new DemoParticipantSession(
                communicator,
                client,
                participantController,
                adapter,
                viewModel);
        }
        catch
        {
            viewModel?.Dispose();
            adapter?.Dispose();
            participantController?.Dispose();
            client?.Dispose();
            communicator.Dispose();
            throw;
        }
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
