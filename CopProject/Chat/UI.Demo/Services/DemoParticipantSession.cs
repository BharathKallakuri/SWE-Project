using Chat;
using System.Net;
using Microsoft.UI.Dispatching;
using Networking.TCPCommunication;
using UI.Services;
using UI.ViewModels;

namespace UI.Demo.Services;

/// <summary>
/// Owns all resources belonging to one participant window.
/// </summary>
public sealed class DemoParticipantSession : IDisposable
{
    private readonly CommunicatorClient _communicator;
    private readonly MockChatClientController _participantController;
    private int _disposed;

    public static DemoParticipantSession Create(
        string username,
        string roomName,
        string hostName,
        IPEndPoint serverEndpoint,
        DispatcherQueue dispatcherQueue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);
        ArgumentNullException.ThrowIfNull(serverEndpoint);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        var communicator = new CommunicatorClient(serverEndpoint);
        if (communicator.ListenPort <= 0)
        {
            communicator.Dispose();
            throw new InvalidOperationException(
                $"Participant '{username}' could not connect to the demo server at {serverEndpoint}.");
        }

        ChatClient? client = null;
        MockChatClientController? participantController = null;
        ChatClientAdapter? adapter = null;
        ChatPanelViewModel? viewModel = null;

        try
        {
            Chatroom localRoom = new(roomName, hostName);
            client = new ChatClient(username, localRoom, communicator, serverEndpoint);
            participantController = new MockChatClientController(
                username,
                communicator,
                serverEndpoint);
            adapter = new ChatClientAdapter(client, dispatcherQueue);
            viewModel = new ChatPanelViewModel(adapter);
            participantController.JoinMeeting();

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

    public DemoParticipantSession(
        CommunicatorClient communicator,
        ChatClient client,
        MockChatClientController participantController,
        ChatClientAdapter adapter,
        ChatPanelViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(communicator);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(participantController);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(viewModel);

        _communicator = communicator;
        _participantController = participantController;
        Client = client;
        Adapter = adapter;
        ViewModel = viewModel;
    }

    public ChatClient Client { get; }

    public ChatClientAdapter Adapter { get; }

    public ChatPanelViewModel ViewModel { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            _participantController.Dispose();
        }
        finally
        {
            ViewModel.Dispose();
            Adapter.Dispose();
            Client.Dispose();
            _communicator.Dispose();
        }
    }
}
