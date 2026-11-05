using Chat;
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
