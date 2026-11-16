// Author: Rajat
// Roll no.: 112301044

using System.Net;
using Chat;
using Controller;
using Insights.AI;
using UI.ViewModels;
using GlobalAdapter = global::UI.Services.ChatClientAdapter;

namespace Chat.UI.Test;

/// <summary>Builds a client-role chat panel wired to in-memory fakes.</summary>
internal sealed class ClientPanel : IDisposable
{
    private ClientPanel(
        RecordingCommunicator communicator,
        RecordingController controller,
        ChatClient client,
        GlobalAdapter adapter,
        ChatPanelViewModel viewModel)
    {
        Communicator = communicator;
        Controller = controller;
        Client = client;
        Adapter = adapter;
        ViewModel = viewModel;
        Server = new FakeServer(communicator, client);
    }

    public const string HostName = "hostuser";
    public const string CurrentUser = "alice";

    public RecordingCommunicator Communicator { get; }
    public RecordingController Controller { get; }
    public ChatClient Client { get; }
    public GlobalAdapter Adapter { get; }
    public ChatPanelViewModel ViewModel { get; }
    public FakeServer Server { get; }

    public static ClientPanel Create(byte[]? currentUserPicture = null)
    {
        Chatroom room = new("meeting", HostName);
        RecordingCommunicator communicator = new();
        RecordingController controller = new(CurrentUser, currentUserPicture) {
            Communicator = communicator
        };
        ChatClient client = new(
            CurrentUser,
            room,
            communicator,
            new IPEndPoint(IPAddress.Loopback, 12345));
        GlobalAdapter adapter = new(client, DispatcherThread.Queue, controller);
        ChatPanelViewModel viewModel = new(adapter);
        viewModel.Initialize();
        return new ClientPanel(communicator, controller, client, adapter, viewModel);
    }

    public void Dispose()
    {
        Dispose(ViewModel, Adapter, Client);
    }

    private static void Dispose(ChatPanelViewModel viewModel, GlobalAdapter adapter, ChatClient client)
    {
        viewModel.Dispose();
        adapter.Dispose();
        client.Dispose();
    }
}

/// <summary>Builds a host-role chat panel wired to in-memory fakes.</summary>
internal sealed class HostPanel : IDisposable
{
    private HostPanel(
        RecordingCommunicator communicator,
        RecordingController controller,
        ChatServer server,
        GlobalAdapter adapter,
        ChatPanelViewModel viewModel)
    {
        Communicator = communicator;
        Controller = controller;
        Server = server;
        Adapter = adapter;
        ViewModel = viewModel;
    }

    public const string HostName = "hostuser";

    public RecordingCommunicator Communicator { get; }
    public RecordingController Controller { get; }
    public ChatServer Server { get; }
    public GlobalAdapter Adapter { get; }
    public ChatPanelViewModel ViewModel { get; }

    public static HostPanel Create()
    {
        Chatroom room = new("meeting", HostName);
        RecordingCommunicator communicator = new();
        RecordingController controller = new(HostName) {
            Communicator = communicator
        };
        ChatServer server = new(
            room,
            controller,
            () => Task.FromResult(new InsightsManager(new InsightsTestProvider("AI answer."))));
        GlobalAdapter adapter = new(server, controller, DispatcherThread.Queue);
        ChatPanelViewModel viewModel = new(adapter);
        viewModel.Initialize();
        return new HostPanel(communicator, controller, server, adapter, viewModel);
    }

    public void Dispose()
    {
        ViewModel.Dispose();
        Adapter.Dispose();
        Server.Dispose();
    }
}
