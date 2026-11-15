using Microsoft.UI.Xaml;
using Chat;
using UI.ViewModels;

namespace Chat.UI.Test;

public class ChatPanelViewModelTests
{
    [Fact]
    public void RefreshButton_IsShownForClient_AndHiddenForHost()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel client = ClientPanel.Create();
            using HostPanel host = HostPanel.Create();

            Assert.False(client.ViewModel.IsHost);
            Assert.Equal(Visibility.Visible, client.ViewModel.RefreshVisibility);
            Assert.True(host.ViewModel.IsHost);
            Assert.Equal(Visibility.Collapsed, host.ViewModel.RefreshVisibility);
        });
    }

    [Fact]
    public void SendMessageCommand_CannotExecute_WhenMessageIsEmptyOrWhitespace()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();
            ChatPanelViewModel viewModel = panel.ViewModel;

            Assert.False(viewModel.SendMessageCommand.CanExecute(null));
            viewModel.MessageText = "   ";
            Assert.False(viewModel.SendMessageCommand.CanExecute(null));
            viewModel.MessageText = "hello";
            Assert.True(viewModel.SendMessageCommand.CanExecute(null));
        });
    }

    [Fact]
    public void ReceivingMessageAdded_AddsMessageToConversation()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();

            panel.Server.AddMessage("hostuser", "first message", out Guid firstId, timestamp: DateTimeOffset.UtcNow.AddMinutes(-2));
            panel.Server.AddMessage(ClientPanel.CurrentUser, "second message", out _, timestamp: DateTimeOffset.UtcNow.AddMinutes(-1));

            Assert.Equal(2, panel.ViewModel.Messages.Count);
            Assert.Equal(firstId, panel.ViewModel.Messages[0].Id);
            Assert.Equal("first message", panel.ViewModel.Messages[0].Content);
            Assert.Equal(Visibility.Collapsed, panel.ViewModel.EmptyConversationVisibility);
        });
    }

    [Fact]
    public void ReceivingOutOfOrderBatch_RequestsFullReset()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();
            panel.Communicator.ClearSent();

            panel.Server.SendBatchWithWrongBaseVersion();

            ChatClientRequestBody request = Assert.Single(panel.Communicator.ReadRequests());
            Assert.IsType<Reset>(request.Request);
        });
    }

    [Fact]
    public void ReceivingReset_ClearsMessages_AndClosesThread()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "root message", out Guid rootId);
            panel.Server.AddMessage("hostuser", "thread reply", out _, threadParentMessageId: rootId);
            MessageViewModel root = panel.ViewModel.Messages.Single(message => message.Id == rootId);
            root.OpenThreadCommand.Execute(null);
            Assert.True(panel.ViewModel.IsThreadOpen);

            panel.Server.SendReset();

            Assert.Empty(panel.ViewModel.Messages);
            Assert.Empty(panel.ViewModel.ThreadMessages);
            Assert.False(panel.ViewModel.IsThreadOpen);
        });
    }

    [Fact]
    public void ParticipantsUpdate_PopulatesParticipantList()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();

            panel.Server.AddMessage("hostuser", "hello", out _);

            IEnumerable<string> usernames = panel.ViewModel.Participants.Select(participant => participant.Username);
            Assert.Contains(ClientPanel.HostName, usernames);
            Assert.Contains(ClientPanel.CurrentUser, usernames);
            Assert.Contains(ChatConstants.AiAssistantUsername, usernames);
        });
    }

    [Fact]
    public void OpeningThread_ShowsReplies_AndClosingReturnsToRoom()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "root", out Guid rootId);
            panel.Server.AddMessage("hostuser", "a reply", out _, threadParentMessageId: rootId);

            MessageViewModel root = panel.ViewModel.Messages.Single(message => message.Id == rootId);
            Assert.Equal(1, root.ReplyCount);
            root.OpenThreadCommand.Execute(null);

            Assert.Same(root, panel.ViewModel.ActiveThreadRoot);
            Assert.Equal(Visibility.Collapsed, panel.ViewModel.ConversationVisibility);
            Assert.Equal(Visibility.Visible, panel.ViewModel.ThreadPanelVisibility);
            Assert.Equal("a reply", Assert.Single(panel.ViewModel.ThreadMessages).Content);

            panel.ViewModel.CloseThreadCommand.Execute(null);

            Assert.Null(panel.ViewModel.ActiveThreadRoot);
            Assert.Equal(Visibility.Visible, panel.ViewModel.ConversationVisibility);
        });
    }

    [Fact]
    public void Client_CannotEditOrDeleteOthersMessages_UnlessHost()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel client = ClientPanel.Create();
            client.Server.AddMessage(ClientPanel.HostName, "host says", out _);
            MessageViewModel message = Assert.Single(client.ViewModel.Messages);

            Assert.False(message.CanEdit);
            Assert.False(message.CanDelete);
            Assert.False(message.EditCommand.CanExecute(null));
            Assert.False(message.DeleteCommand.CanExecute(null));
        });
    }

    [Fact]
    public void Host_CanEditAndDeleteOtherParticipantsMessages()
    {
        DispatcherThread.Run(() =>
        {
            using HostPanel panel = HostPanel.Create();
            panel.Controller.AddUser("bob");
            panel.Server.RegisterClient("bob");

            panel.Server.HandleRequest(new ChatClientRequestEnvelope(
                Guid.NewGuid(),
                new SendMessage(DateTimeOffset.UtcNow, new Mentions(), "bob says", null),
                "bob"));

            MessageViewModel bobMessage = Assert.Single(panel.ViewModel.Messages);
            Assert.False(bobMessage.IsOwnMessage);
            Assert.True(bobMessage.CanEdit); // host may moderate anyone's messages
            Assert.True(bobMessage.CanDelete);
        });
    }

    [Fact]
    public void MessageCreate_UsesControllerProfilePictureAndEmail_AsAvatarAndTooltip()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create(TestImages.OnePixelPng);

            panel.Server.AddMessage(ClientPanel.CurrentUser, "with picture", out _);

            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            Assert.NotNull(message.AvatarImageSource);
            Assert.Contains("alice@example.com", message.AvatarToolTip);
        });
    }

    [Fact]
    public void Avatar_PicksUpLateJoiningParticipantsPicture_WhenControllerNotified()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("bob", "before join", out _);
            Assert.Null(Assert.Single(panel.ViewModel.Messages).AvatarImageSource);

            panel.Controller.RaiseUserJoined("bob", TestImages.OnePixelPng);

            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            Assert.NotNull(message.AvatarImageSource);
            Assert.Contains("bob@example.com", message.AvatarToolTip);
        });
    }

    [Fact]
    public void MessageWithoutControllerRecord_ShowsInitials_AndNoTooltip()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create();

            panel.Server.AddMessage("mystery", "who am i", out _);

            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            Assert.Null(message.AvatarImageSource);
            Assert.Null(message.AvatarToolTip);
            Assert.Equal(Visibility.Visible, message.InitialsVisibility);
        });
    }

    [Fact]
    public void Initialize_RequestsSynchronizationOfChatHistory()
    {
        DispatcherThread.Run(() =>
        {
            using ClientPanel panel = ClientPanel.Create(); // Create() runs Initialize()

            ChatClientRequestBody request = Assert.Single(panel.Communicator.ReadRequests());
            Assert.IsType<Sync>(request.Request);
            Assert.Equal(ClientPanel.CurrentUser, request.Username);
        });
    }
}
