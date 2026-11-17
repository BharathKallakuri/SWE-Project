// Author: Rajat
// Roll no.: 112301044

using Microsoft.UI.Xaml;
using UI.ViewModels;

namespace Chat.UI.Test;

public class ChatPanelViewModelTests
{
    [Fact]
    public void RefreshButton_IsShownForClient_AndHiddenForHost()
    {
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
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
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();

            panel.Server.AddMessage("mystery", "who am i", out _);

            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            Assert.Null(message.AvatarImageSource);
            Assert.Null(message.AvatarToolTip);
            Assert.Equal(Visibility.Visible, message.InitialsVisibility);
        });
    }

    [Fact]
    public void RefreshButton_SendsProtocolResetRequest_WhenClickedByClient()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Communicator.ClearSent();

            panel.ViewModel.ResetCommand.Execute(null);

            ChatClientRequestBody request = Assert.Single(panel.Communicator.ReadRequests());
            Assert.IsType<Reset>(request.Request);
            Assert.Contains("reset", panel.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void SendMessageCommand_SendsMessage_AndClearsText_WhenServerConfirms()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            ChatPanelViewModel viewModel = panel.ViewModel;
            viewModel.MessageText = "hello everyone";
            panel.Communicator.ClearSent();

            viewModel.SendMessageCommand.Execute(null);

            ChatClientRequestBody request = Assert.Single(panel.Communicator.ReadRequests());
            SendMessage sent = Assert.IsType<SendMessage>(request.Request);
            Assert.Equal("hello everyone", sent.Content);
            Assert.Contains("queued", viewModel.StatusMessage);
            Assert.Equal("hello everyone", viewModel.MessageText); // not cleared before confirmation

            panel.Server.SendRequestResult(request.RequestId, RoomResult.Ok);

            Assert.Equal(string.Empty, viewModel.MessageText);
            Assert.Contains("accepted", viewModel.StatusMessage);
        });
    }

    [Fact]
    public void SendMessageCommand_ShowsError_WhenNotFound()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            ChatPanelViewModel viewModel = panel.ViewModel;
            viewModel.MessageText = "hello";
            panel.Communicator.ClearSent();

            viewModel.SendMessageCommand.Execute(null);

            ChatClientRequestBody request = panel.Communicator.ReadRequests().Single();
            panel.Server.SendRequestResult(request.RequestId, RoomResult.NotFound);

            Assert.Contains("not found", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void SendMessageCommand_SendsMentionAll_WhenAllMentionSelected()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();

            // ChAI and @all are always present in the mention targets.
            MentionTargetViewModel all = Assert.Single(panel.ViewModel.MentionTargets, target => target.Username == "@all");
            all.ToggleCommand.Execute(null);
            panel.ViewModel.MessageText = "attention please";
            panel.Communicator.ClearSent();
            panel.ViewModel.SendMessageCommand.Execute(null);

            SendMessage sent = Assert.IsType<SendMessage>(Assert.Single(panel.Communicator.ReadRequests()).Request);
            Assert.True(sent.Mentions.All);
        });
    }

    [Fact]
    public void MentionTarget_TogglesSelectAndDeselect_UpdatesSelection()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();

            MentionTargetViewModel host = Assert.Single(panel.ViewModel.MentionTargets, t => t.Username == ClientPanel.HostName);
            host.ToggleCommand.Execute(null);
            Assert.Contains(panel.ViewModel.SelectedMentions, t => t.Username == ClientPanel.HostName);
            Assert.Equal("✓", host.SelectionMark);

            host.ToggleCommand.Execute(null);
            Assert.DoesNotContain(panel.ViewModel.SelectedMentions, t => t.Username == ClientPanel.HostName);
            Assert.Equal("+", host.SelectionMark);
        });
    }

    [Fact]
    public void MentionSearch_NarrowsResults_AndIsCaseInsensitive()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();

            panel.ViewModel.MentionSearchText = "HOST";

            Assert.Contains(panel.ViewModel.MentionSearchResults, target => target.Username == ClientPanel.HostName);
            Assert.DoesNotContain(panel.ViewModel.MentionSearchResults, target => target.Username == ChatConstants.AiAssistantUsername);

            panel.ViewModel.MentionSearchText = string.Empty;

            Assert.Contains(panel.ViewModel.MentionSearchResults, target => target.Username == ChatConstants.AiAssistantUsername);
        });
    }

    [Fact]
    public void ClickingReply_ShowsReplyBanner_AndCancelHidesIt()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "a question", out _);
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);

            message.ReplyInChatCommand.Execute(null);

            Assert.Same(message, panel.ViewModel.ReplyingToMessage);
            Assert.Equal(Visibility.Visible, panel.ViewModel.ReplyBannerVisibility);
            Assert.Contains("hostuser", panel.ViewModel.ReplyingToText);

            panel.ViewModel.CancelChatReplyCommand.Execute(null);

            Assert.Null(panel.ViewModel.ReplyingToMessage);
            Assert.Equal(Visibility.Collapsed, panel.ViewModel.ReplyBannerVisibility);
        });
    }

    [Fact]
    public void ClickingEdit_OpensEditComposer_AndPrefillsCurrentText()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage(ClientPanel.CurrentUser, "before edit", out Guid messageId);
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);

            message.EditCommand.Execute(null);

            Assert.NotNull(panel.ViewModel.EditingMessage);
            Assert.Equal(messageId, panel.ViewModel.EditingMessage!.Id);
            Assert.Equal("before edit", panel.ViewModel.EditText);
            Assert.Equal(Visibility.Visible, panel.ViewModel.EditComposerVisibility);
            Assert.Equal(Visibility.Collapsed, panel.ViewModel.MainComposerVisibility);
        });
    }

    [Fact]
    public void SavingEdit_SendsEditRequest_AndClosesEditor_WhenServerConfirms()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage(ClientPanel.CurrentUser, "before edit", out Guid messageId);
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            message.EditCommand.Execute(null);
            panel.ViewModel.EditText = "after edit";
            panel.Communicator.ClearSent();

            panel.ViewModel.SaveEditCommand.Execute(null);

            EditMessage sent = Assert.IsType<EditMessage>(Assert.Single(panel.Communicator.ReadRequests()).Request);
            Assert.Equal(messageId, sent.MessageId);
            Assert.Equal("after edit", sent.NewContent);

            panel.Server.SendRequestResult(Assert.Single(panel.Communicator.ReadRequests()).RequestId, RoomResult.Ok);

            Assert.Null(panel.ViewModel.EditingMessage);
            Assert.Equal(Visibility.Collapsed, panel.ViewModel.EditComposerVisibility);
        });
    }

    [Fact]
    public void EditingCancel_ClearsEditingState()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage(ClientPanel.CurrentUser, "before edit", out _);
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            message.EditCommand.Execute(null);
            Assert.NotNull(panel.ViewModel.EditingMessage);

            panel.ViewModel.CancelEditCommand.Execute(null);

            Assert.Null(panel.ViewModel.EditingMessage);
            Assert.Equal(Visibility.Visible, panel.ViewModel.MainComposerVisibility);
        });
    }

    [Fact]
    public void ClosingThread_ClearsEditingState_AndThreadMessages()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage(ClientPanel.CurrentUser, "root", out Guid rootId);
            panel.Server.AddMessage("hostuser", "thread reply", out _, threadParentMessageId: rootId);

            MessageViewModel root = panel.ViewModel.Messages.Single(item => item.Id == rootId);
            root.EditCommand.Execute(null); // open editor on the root
            Assert.NotNull(panel.ViewModel.EditingMessage);

            root.OpenThreadCommand.Execute(null);

            Assert.Null(panel.ViewModel.EditingMessage);
            Assert.Equal(string.Empty, panel.ViewModel.EditText);
            Assert.True(panel.ViewModel.IsThreadOpen);
            Assert.Single(panel.ViewModel.ThreadMessages);
        });
    }

    [Fact]
    public void ClickingDelete_SendsDeleteRequest()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage(ClientPanel.CurrentUser, "delete me", out Guid messageId);
            panel.Communicator.ClearSent();
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);

            message.DeleteCommand.Execute(null);

            DeleteMessage sent = Assert.IsType<DeleteMessage>(Assert.Single(panel.Communicator.ReadRequests()).Request);
            Assert.Equal(messageId, sent.MessageId);
        });
    }

    [Fact]
    public void ClickingReaction_SendsToggleReactionRequest()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "react to this", out Guid messageId);
            panel.Communicator.ClearSent();
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);

            message.ToggleReactionCommand.Execute("👍");

            ToggleReaction sent = Assert.IsType<ToggleReaction>(Assert.Single(panel.Communicator.ReadRequests()).Request);
            Assert.Equal(messageId, sent.MessageId);
            Assert.Equal("👍", sent.Emoji);
        });
    }

    [Fact]
    public void ReplyPreview_ShowsReferencedMessageContent()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "the referenced message", out Guid referencedId);
            panel.Server.AddMessage(
                "hostuser",
                "a reply",
                out Guid replyId,
                replyToMessageId: referencedId);

            MessageViewModel reply = panel.ViewModel.Messages.Single(item => item.Id == replyId);

            Assert.Equal(Visibility.Visible, reply.ReplyPreviewVisibility);
            Assert.Contains("hostuser", reply.ReplyPreviewText);
            Assert.Contains("the referenced message", reply.ReplyPreviewText);
        });
    }

    [Fact]
    public void ReplyPreview_Updates_WhenTargetIsEdited()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "original text", out Guid referencedId);
            panel.Server.AddMessage(
                "hostuser",
                "a reply",
                out Guid replyId,
                replyToMessageId: referencedId);
            MessageViewModel reply = panel.ViewModel.Messages.Single(item => item.Id == replyId);

            panel.Server.EditMessage(referencedId, "edited text", "hostuser");

            Assert.Contains("edited text", reply.ReplyPreviewText);
            Assert.DoesNotContain("original text", reply.ReplyPreviewText);
        });
    }

    [Fact]
    public void ReplyPreview_ShowsDeletionNotice_WhenTargetIsDeleted()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "original text", out Guid referencedId);
            panel.Server.AddMessage(
                "hostuser",
                "a reply",
                out Guid replyId,
                replyToMessageId: referencedId);
            MessageViewModel reply = panel.ViewModel.Messages.Single(item => item.Id == replyId);

            panel.Server.DeleteMessage(referencedId, "hostuser");

            Assert.Contains("Message deleted", reply.ReplyPreviewText);
        });
    }

    [Fact]
    public void ReplyPreview_TruncatesLongContent()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            string longContent = new string('x', 200);
            panel.Server.AddMessage("hostuser", longContent, out Guid referencedId);
            panel.Server.AddMessage(
                "hostuser",
                "a reply",
                out Guid replyId,
                replyToMessageId: referencedId);

            MessageViewModel reply = panel.ViewModel.Messages.Single(item => item.Id == replyId);

            Assert.EndsWith("...", reply.ReplyPreviewText);
            Assert.True(reply.ReplyPreviewText.Length <= 120);
        });
    }

    [Fact]
    public void JumpToReply_RaisesScrollToMessageRequested()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "original text", out Guid referencedId);
            panel.Server.AddMessage(
                "hostuser",
                "a reply",
                out Guid replyId,
                replyToMessageId: referencedId);
            MessageViewModel reply = panel.ViewModel.Messages.Single(item => item.Id == replyId);

            List<Guid> requested = new();
            panel.ViewModel.ScrollToMessageRequested += requested.Add;

            reply.JumpToReplyCommand.Execute(null);

            Assert.Single(requested);
            Assert.Equal(referencedId, requested[0]);
        });
    }

    [Fact]
    public void Initialize_RequestsSynchronizationOfChatHistory()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create(); // Create() runs Initialize()

            ChatClientRequestBody request = Assert.Single(panel.Communicator.ReadRequests());
            Assert.IsType<Sync>(request.Request);
            Assert.Equal(ClientPanel.CurrentUser, request.Username);
        });
    }

    [Fact]
    public void Initialize_CalledTwice_OnlySynchronizesOnce()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Communicator.ClearSent();

            panel.ViewModel.Initialize();

            Assert.Empty(panel.Communicator.ReadRequests());
            Assert.Equal(Visibility.Visible, panel.ViewModel.EmptyConversationVisibility);
        });
    }

    [Fact]
    public void SyncCommand_SendsSynchronizationRequest()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Communicator.ClearSent();

            panel.ViewModel.SyncCommand.Execute(null);

            ChatClientRequestBody request = Assert.Single(panel.Communicator.ReadRequests());
            Assert.IsType<Sync>(request.Request);
        });
    }

    [Fact]
    public void ReactionToggled_IsAppliedToExistingMessage()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "react to this", out Guid messageId);

            panel.Server.ToggleReaction(messageId, "👍", "bob");

            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            ReactionViewModel reaction = Assert.Single(message.Reactions);
            Assert.Equal("👍", reaction.Emoji);
            Assert.Equal(1, reaction.Count);
        });
    }

    [Fact]
    public void ThreadReply_CanBeEditedAndDeleted_ByItsAuthor()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "root", out Guid rootId);
            panel.Server.AddMessage(ClientPanel.CurrentUser, "my reply", out Guid replyId, threadParentMessageId: rootId);
            MessageViewModel root = panel.ViewModel.Messages.Single(message => message.Id == rootId);
            root.OpenThreadCommand.Execute(null);
            MessageViewModel reply = Assert.Single(panel.ViewModel.ThreadMessages);

            reply.EditCommand.Execute(null);
            Assert.Equal(replyId, panel.ViewModel.EditingMessage!.Id);
            panel.ViewModel.EditText = "edited reply";
            panel.Communicator.ClearSent();
            panel.ViewModel.SaveEditCommand.Execute(null);

            EditMessage sent = Assert.IsType<EditMessage>(Assert.Single(panel.Communicator.ReadRequests()).Request);
            Assert.Equal(rootId, sent.ThreadParentMessageId);

            panel.Server.EditMessage(replyId, "edited reply", ClientPanel.CurrentUser, rootId);
            Assert.Equal("edited reply", reply.Content);
        });
    }

    [Fact]
    public void MessagesWithIdenticalTimestamps_AreOrderedById()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            DateTimeOffset timestamp = DateTimeOffset.UtcNow.AddMinutes(-5);
            panel.Server.AddMessage("hostuser", "first", out Guid firstId, timestamp: timestamp);
            panel.Server.AddMessage("hostuser", "second", out Guid secondId, timestamp: timestamp);

            // Same timestamp: insertion order must fall back to Id order.
            Assert.Equal(
                panel.ViewModel.Messages.OrderBy(message => message.Id).Select(message => message.Id),
                panel.ViewModel.Messages.Select(message => message.Id));
        });
    }

    [Fact]
    public void ParticipantsRefresh_PreservesSelectedMentions()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            MentionTargetViewModel host = panel.ViewModel.MentionTargets.Single(target => target.Username == ClientPanel.HostName);
            host.ToggleCommand.Execute(null);
            Assert.Single(panel.ViewModel.SelectedMentions);

            panel.Server.AddMessage("hostuser", "trigger a participants refresh", out _);

            Assert.Contains(panel.ViewModel.SelectedMentions, target => target.Username == ClientPanel.HostName);
            Assert.Equal("✓", panel.ViewModel.MentionTargets.Single(target => target.Username == ClientPanel.HostName).SelectionMark);
        });
    }

    [Fact]
    public void ThreadReply_CanExecuteOnlyWithText_AndSendsWithParentId()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "root", out Guid rootId);
            MessageViewModel root = panel.ViewModel.Messages.Single(message => message.Id == rootId);

            Assert.False(panel.ViewModel.SendThreadReplyCommand.CanExecute(null)); // no thread open

            root.OpenThreadCommand.Execute(null);
            Assert.Equal($"Thread · {root.SenderName}", panel.ViewModel.ThreadHeaderText);
            Assert.Equal(Visibility.Visible, panel.ViewModel.ThreadComposerVisibility);
            Assert.Equal(Visibility.Collapsed, panel.ViewModel.StatusVisibility);
            Assert.False(panel.ViewModel.SendThreadReplyCommand.CanExecute(null)); // empty text

            panel.ViewModel.ThreadMessageText = "   ";
            Assert.False(panel.ViewModel.SendThreadReplyCommand.CanExecute(null));

            panel.ViewModel.ThreadMessageText = "thread hello";
            Assert.True(panel.ViewModel.SendThreadReplyCommand.CanExecute(null));
            panel.Communicator.ClearSent();
            panel.ViewModel.SendThreadReplyCommand.Execute(null);

            SendMessage sent = Assert.IsType<SendMessage>(Assert.Single(panel.Communicator.ReadRequests()).Request);
            Assert.Equal("thread hello", sent.Content);
            Assert.Equal(rootId, sent.ThreadParentMessageId);

            panel.Server.SendRequestResult(Assert.Single(panel.Communicator.ReadRequests()).RequestId, RoomResult.Ok);
            Assert.Equal(string.Empty, panel.ViewModel.ThreadMessageText);

            panel.ViewModel.CloseThreadCommand.Execute(null);
            Assert.Equal("Thread", panel.ViewModel.ThreadHeaderText);
        });
    }

    [Fact]
    public void SendMessage_WithAllAndUser_Mentions_ExcludesAllFromUsernames()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            MentionTargetViewModel all = panel.ViewModel.MentionTargets.Single(target => target.Username == "@all");
            MentionTargetViewModel host = panel.ViewModel.MentionTargets.Single(target => target.Username == ClientPanel.HostName);
            all.ToggleCommand.Execute(null);
            host.ToggleCommand.Execute(null);
            panel.ViewModel.MessageText = "hi";
            panel.Communicator.ClearSent();

            panel.ViewModel.SendMessageCommand.Execute(null);

            SendMessage sent = Assert.IsType<SendMessage>(Assert.Single(panel.Communicator.ReadRequests()).Request);
            Assert.True(sent.Mentions.All);
            Assert.Contains(ClientPanel.HostName, sent.Mentions.Usernames);
            Assert.DoesNotContain("@all", sent.Mentions.Usernames);

            panel.Server.SendRequestResult(Assert.Single(panel.Communicator.ReadRequests()).RequestId, RoomResult.Ok);

            Assert.Empty(panel.ViewModel.SelectedMentions);
            Assert.Equal("+", all.SelectionMark);
            Assert.Equal("+", host.SelectionMark);
        });
    }

    [Fact]
    public void ParticipantsRefresh_WhileSynchronizing_ShowsConnectedSummary()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            Assert.StartsWith("Synchronizing", panel.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);

            panel.Server.AddMessage("hostuser", "trigger", out _);

            Assert.StartsWith("Connected ·", panel.ViewModel.StatusMessage);
        });
    }

    [Fact]
    public void ReplyInChat_FromOpenThread_ClosesThread_AndSetsReplyTarget()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage("hostuser", "root", out Guid rootId);
            panel.Server.AddMessage("hostuser", "other message", out Guid otherId);
            MessageViewModel root = panel.ViewModel.Messages.Single(message => message.Id == rootId);
            MessageViewModel other = panel.ViewModel.Messages.Single(message => message.Id == otherId);
            root.OpenThreadCommand.Execute(null);
            Assert.True(panel.ViewModel.IsThreadOpen);

            other.ReplyInChatCommand.Execute(null);

            Assert.False(panel.ViewModel.IsThreadOpen);
            Assert.Same(other, panel.ViewModel.ReplyingToMessage);
        });
    }

    [Fact]
    public void SaveEdit_WithWhitespace_CannotExecute_AndSendsNothing()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Server.AddMessage(ClientPanel.CurrentUser, "before edit", out _);
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            message.EditCommand.Execute(null);
            panel.ViewModel.EditText = "   ";
            panel.Communicator.ClearSent();

            Assert.False(panel.ViewModel.SaveEditCommand.CanExecute(null));
            panel.ViewModel.SaveEditCommand.Execute(null);

            Assert.Empty(panel.Communicator.ReadRequests());
        });
    }

    [Fact]
    public void SetUserAvatar_ClearsThenRestores_AvatarForParticipantAndMessage()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create(TestImages.OnePixelPng);
            panel.Server.AddMessage(ClientPanel.CurrentUser, "with picture", out _);
            MessageViewModel message = Assert.Single(panel.ViewModel.Messages);
            Assert.NotNull(message.AvatarImageSource);

            panel.ViewModel.SetUserAvatar(ClientPanel.CurrentUser, null);

            Assert.Null(message.AvatarImageSource);
            ParticipantViewModel participant = panel.ViewModel.Participants.Single(p => p.Username == ClientPanel.CurrentUser);
            Assert.Null(participant.AvatarImageSource);

            panel.ViewModel.SetUserAvatar(ClientPanel.CurrentUser, new Microsoft.UI.Xaml.Media.Imaging.BitmapImage());

            Assert.NotNull(message.AvatarImageSource);
            Assert.NotNull(participant.AvatarImageSource);
        });
    }

    [Fact]
    public void RequestCompleted_ForbiddenAndUnknownResults_ShowStatus()
    {
        DispatcherThread.Run(() => {
            using ClientPanel panel = ClientPanel.Create();
            panel.Communicator.ClearSent();

            panel.ViewModel.MessageText = "hello";
            panel.ViewModel.SendMessageCommand.Execute(null);
            Guid forbiddenId = Assert.Single(panel.Communicator.ReadRequests()).RequestId;
            panel.Server.SendRequestResult(forbiddenId, RoomResult.Forbidden);
            Assert.Contains("permission", panel.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);

            panel.ViewModel.MessageText = "hello again";
            panel.Communicator.ClearSent();
            panel.ViewModel.SendMessageCommand.Execute(null);
            Guid unknownId = Assert.Single(panel.Communicator.ReadRequests()).RequestId;
            panel.Server.SendRequestResult(unknownId, (RoomResult)999);
            Assert.Contains("unknown", panel.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        });
    }
}
