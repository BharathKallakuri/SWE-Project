// Author: Rajat
// Roll no.: 112301044

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Chat;
using UI.ViewModels;

namespace Chat.UI.Test;

public class MessageViewModelTests
{
    private static MessageViewModel CreateMessage(
        string sender,
        string currentUser = "alice",
        string hostName = "hostuser",
        Mentions? mentions = null,
        Guid? replyToMessageId = null)
    {
        ChatMessage message = new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            SenderName = sender,
            Content = "hello there",
            Mentions = mentions ?? new Mentions(),
            ParentMessageId = replyToMessageId
        };

        return new MessageViewModel(
            Guid.NewGuid(),
            message,
            currentUser,
            hostName,
            _ => { },
            _ => { },
            _ => { },
            _ => { },
            (_, _) => { },
            _ => { });
    }

    [Fact]
    public void OwnMessage_AlignsRight_AndHidesLeftAvatar()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel own = CreateMessage("alice", currentUser: "alice");

            Assert.True(own.IsOwnMessage);
            Assert.Equal(HorizontalAlignment.Right, own.BubbleAlignment);
            Assert.Equal(Visibility.Collapsed, own.LeftAvatarVisibility);
            Assert.Equal(Visibility.Visible, own.RightAvatarVisibility);
        });
    }

    [Fact]
    public void OtherUsersMessage_AlignsLeft_AndHidesRightAvatar()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel other = CreateMessage("hostuser", currentUser: "alice");

            Assert.False(other.IsOwnMessage);
            Assert.Equal(HorizontalAlignment.Left, other.BubbleAlignment);
            Assert.Equal(Visibility.Visible, other.LeftAvatarVisibility);
            Assert.Equal(Visibility.Collapsed, other.RightAvatarVisibility);
        });
    }

    [Fact]
    public void OtherUsersMessage_CannotBeEditedOrDeleted_ByRegularParticipant()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel other = CreateMessage("hostuser", currentUser: "alice", hostName: "hostuser");

            Assert.False(other.CanEdit);
            Assert.False(other.CanDelete);
        });
    }

    [Fact]
    public void CurrentUser_CanEditAndDelete_OwnMessages()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel own = CreateMessage("alice", currentUser: "alice");

            Assert.True(own.CanEdit);
            Assert.True(own.CanDelete);
        });
    }

    [Fact]
    public void Host_CanEditAndDelete_RegularParticipantsMessages()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel fromAlice = CreateMessage("alice", currentUser: "hostuser", hostName: "hostuser");

            Assert.True(fromAlice.IsCurrentUserHost);
            Assert.True(fromAlice.CanEdit);
            Assert.True(fromAlice.CanDelete);
        });
    }

    [Fact]
    public void MessageMentioningCurrentUser_ShowsMentionLabel()
    {
        DispatcherThread.Run(() =>
        {
            Mentions mentions = new();
            mentions.Usernames.Add("alice");
            MessageViewModel message = CreateMessage("hostuser", currentUser: "alice", mentions: mentions);

            Assert.True(message.IsMentioned);
            Assert.Equal("✦ YOU WERE MENTIONED", message.MentionLabel);
            Assert.Equal(Visibility.Visible, message.MentionVisibility);
        });
    }

    [Fact]
    public void MessageNotMentioningCurrentUser_HidesMentionLabel()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("hostuser", currentUser: "alice");

            Assert.False(message.IsMentioned);
            Assert.Equal(Visibility.Collapsed, message.MentionVisibility);
        });
    }

    [Fact]
    public void DeletingMessage_DisablesActions_AndShowsDeletionNotice()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("alice", currentUser: "alice");

            message.ApplyDeletion("hostuser");

            Assert.True(message.IsDeleted);
            Assert.Equal("Message deleted by host", message.DisplayContent);
            Assert.False(message.CanEdit);
            Assert.False(message.CanDelete);
            Assert.False(message.CanReplyInChat);
            Assert.Equal(Visibility.Collapsed, message.MoreActionsVisibility);
            Assert.Equal(Visibility.Collapsed, message.ReplyInChatVisibility);
        });
    }

    [Fact]
    public void DeletingMessage_ByRegularParticipant_NamesTheDeleter()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("alice", currentUser: "alice");

            message.ApplyDeletion("bob");

            Assert.Equal("Message deleted by bob", message.DisplayContent);
        });
    }

    [Fact]
    public void Reactions_AddAndRemove_AsToggled()
    {
        DispatcherThread.Run(() =>
        {
            List<string> reactionsSent = new();
            MessageViewModel message = CreateReactionMessage(reactionsSent.Add);

            message.ApplyReaction("👍", "bob");
            Assert.Single(message.Reactions);
            Assert.Equal(1, message.Reactions[0].Count);

            message.ApplyReaction("👍", "carol");
            Assert.Single(message.Reactions);
            Assert.Equal(2, message.Reactions[0].Count);

            message.ApplyReaction("👍", "bob"); // toggling bob off leaves carol
            Assert.Single(message.Reactions);
            Assert.Equal(1, message.Reactions[0].Count);

            message.ApplyReaction("👍", "carol"); // toggling carol off removes the reaction entirely
            Assert.Empty(message.Reactions);
        });
    }

    [Fact]
    public void MentionLabel_ForOtherParticipants_ListsRecipients()
    {
        DispatcherThread.Run(() =>
        {
            Mentions mentions = new();
            mentions.Usernames.Add("bob");
            mentions.Usernames.Add(ChatConstants.AiAssistantUsername);
            MessageViewModel message = CreateMessage("hostuser", currentUser: "alice", mentions: mentions);

            Assert.False(message.IsMentioned);
            Assert.True(message.HasMentions);
            Assert.StartsWith("To · ", message.MentionLabel);
            Assert.Contains("bob", message.MentionLabel);
            Assert.Contains(ChatConstants.AiAssistantUsername, message.MentionLabel);
            Assert.Equal(Visibility.Visible, message.MentionVisibility);
        });
    }

    [Fact]
    public void MentionLabel_WithoutMentions_IsEmptyAndHidden()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("hostuser", currentUser: "alice");

            Assert.False(message.HasMentions);
            Assert.Equal(string.Empty, message.MentionLabel);
            Assert.Equal(Visibility.Collapsed, message.MentionVisibility);
        });
    }

    [Fact]
    public void SendCustomReaction_WithSingleEmoji_TogglesReactionAndClearsText()
    {
        DispatcherThread.Run(() =>
        {
            List<string> reactions = new();
            MessageViewModel message = CreateReactionMessage(reactions.Add);

            message.CustomReactionText = "👍";
            Assert.True(message.SendCustomReactionCommand.CanExecute(null));

            message.SendCustomReactionCommand.Execute(null);

            Assert.Equal(new[] { "👍" }, reactions);
            Assert.Equal(string.Empty, message.CustomReactionText);
        });
    }

    [Fact]
    public void SendCustomReaction_WithSymbolRangeEmoji_IsAccepted()
    {
        DispatcherThread.Run(() =>
        {
            List<string> reactions = new();
            MessageViewModel message = CreateReactionMessage(reactions.Add);

            message.CustomReactionText = "⚡"; // U+26A1, inside the 0x2300-0x27FF emoji range
            Assert.True(message.SendCustomReactionCommand.CanExecute(null));

            message.SendCustomReactionCommand.Execute(null);

            Assert.Equal(new[] { "⚡" }, reactions);
        });
    }

    [Fact]
    public void SendCustomReaction_RejectsMultipleCharsAndNonEmoji()
    {
        DispatcherThread.Run(() =>
        {
            List<string> reactions = new();
            MessageViewModel message = CreateReactionMessage(reactions.Add);

            message.CustomReactionText = "ab";
            Assert.False(message.SendCustomReactionCommand.CanExecute(null));
            message.CustomReactionText = "a";
            Assert.False(message.SendCustomReactionCommand.CanExecute(null));
            message.CustomReactionText = "   ";
            Assert.False(message.SendCustomReactionCommand.CanExecute(null));

            message.SendCustomReactionCommand.Execute(null);
            Assert.Empty(reactions);
        });
    }

    [Fact]
    public void ToggleReactionCommand_GatingAndIgnoresBadParameters()
    {
        DispatcherThread.Run(() =>
        {
            List<string> reactions = new();
            MessageViewModel message = CreateReactionMessage(reactions.Add);

            Assert.True(message.ToggleReactionCommand.CanExecute("👍"));
            Assert.False(message.ToggleReactionCommand.CanExecute(null));
            Assert.False(message.ToggleReactionCommand.CanExecute(string.Empty));
            Assert.False(message.ToggleReactionCommand.CanExecute("   "));
            Assert.False(message.ToggleReactionCommand.CanExecute(42));

            message.ToggleReactionCommand.Execute(42);
            message.ToggleReactionCommand.Execute("   ");
            Assert.Empty(reactions);

            message.ToggleReactionCommand.Execute("👍");
            Assert.Equal(new[] { "👍" }, reactions);
        });
    }

    [Fact]
    public void ReactionPicker_Toggles_AndDeletionDisablesIt()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("alice", currentUser: "alice");

            Assert.False(message.IsReactionPickerOpen);
            Assert.Equal(Visibility.Collapsed, message.ReactionPickerVisibility);

            message.ToggleReactionPickerCommand.Execute(null);
            Assert.True(message.IsReactionPickerOpen);
            Assert.Equal(Visibility.Visible, message.ReactionPickerVisibility);

            message.ToggleReactionPickerCommand.Execute(null);
            Assert.False(message.IsReactionPickerOpen);
            Assert.Equal(Visibility.Collapsed, message.ReactionPickerVisibility);

            message.ToggleReactionPickerCommand.Execute(null);
            message.ApplyDeletion("hostuser");
            Assert.False(message.IsReactionPickerOpen);
            Assert.Equal(Visibility.Collapsed, message.ReactionPickerVisibility);
            Assert.False(message.ToggleReactionPickerCommand.CanExecute(null));
            Assert.False(message.ToggleReactionCommand.CanExecute("👍"));
            Assert.False(message.SendCustomReactionCommand.CanExecute(null));
            Assert.False(message.CanReplyInChat);

            // Applying a deletion twice is a no-op.
            message.ApplyDeletion("bob");
            Assert.Equal("Message deleted by host", message.DeletedMessageText);
        });
    }

    [Fact]
    public void ThreadedMessage_SeedsReplyCountFromExistingReplies()
    {
        DispatcherThread.Run(() =>
        {
            ChatMessageWithThreads threaded = new()
            {
                Timestamp = DateTimeOffset.UtcNow,
                SenderName = "hostuser",
                Content = "root",
                Mentions = new Mentions()
            };
            threaded.ThreadById.Add(
                Guid.NewGuid(),
                new ChatMessage { Timestamp = DateTimeOffset.UtcNow, SenderName = "alice", Content = "one", Mentions = new Mentions() });
            threaded.ThreadById.Add(
                Guid.NewGuid(),
                new ChatMessage { Timestamp = DateTimeOffset.UtcNow, SenderName = "bob", Content = "two", Mentions = new Mentions() });

            MessageViewModel message = new(
                Guid.NewGuid(),
                threaded,
                "alice",
                "hostuser",
                _ => { },
                _ => { },
                _ => { },
                _ => { },
                (_, _) => { },
                _ => { });

            Assert.Equal(2, message.ReplyCount);
            Assert.Equal("2 replies", message.ReplyCountLabel);
            Assert.True(message.HasReplies);
            Assert.Equal(Visibility.Visible, message.ReplyCountVisibility);

            message.IncrementReplyCount();
            Assert.Equal("3 replies", message.ReplyCountLabel);
        });
    }

    [Fact]
    public void SingularReply_UsesSingularLabel()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("hostuser", currentUser: "alice");
            Assert.Equal("0 replies", message.ReplyCountLabel);

            message.IncrementReplyCount();

            Assert.Equal("1 reply", message.ReplyCountLabel);
            Assert.True(message.HasReplies);
        });
    }

    [Fact]
    public void ThreadReply_HidesThreadAffordances()
    {
        DispatcherThread.Run(() =>
        {
            ChatMessage message = new()
            {
                Timestamp = DateTimeOffset.UtcNow,
                SenderName = "alice",
                Content = "thread reply",
                Mentions = new Mentions()
            };
            MessageViewModel reply = new(
                Guid.NewGuid(),
                message,
                "alice",
                "hostuser",
                _ => { },
                _ => { },
                _ => { },
                _ => { },
                (_, _) => { },
                _ => { },
                threadParentMessageId: Guid.NewGuid());

            Assert.False(reply.CanOpenThread);
            Assert.Equal(Visibility.Collapsed, reply.OpenThreadVisibility);
            Assert.False(reply.CanReplyInChat);
            Assert.Equal(Visibility.Collapsed, reply.ReplyInChatVisibility);
        });
    }

    [Fact]
    public void ApplyEdit_UpdatesContent_And_IsIgnoredWhenDeleted()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("alice", currentUser: "alice");
            Assert.False(message.IsEdited);
            Assert.Equal(string.Empty, message.EditedLabel);

            message.ApplyEdit("edited once");
            Assert.Equal("edited once", message.Content);
            Assert.Equal("edited once", message.DisplayContent);
            Assert.Equal(1, message.EditCount);
            Assert.True(message.IsEdited);
            Assert.Equal("edited · 1×", message.EditedLabel);

            message.ApplyDeletion("hostuser");
            message.ApplyEdit("ignored");
            Assert.Equal("edited once", message.Content);
            Assert.Equal(1, message.EditCount);
        });
    }

    [Fact]
    public void DeletedMessage_PresentsDeletedContentStyling()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("alice", currentUser: "alice");

            message.ApplyDeletion("hostuser");

            Assert.Equal(FontStyle.Italic, message.ContentFontStyle);
            Assert.Equal(0.82, message.MessageOpacity);
            Assert.NotNull(message.ContentForeground);
            Assert.NotNull(message.BubbleBrush);
            Assert.NotNull(message.DeletedMessageText);
        });
    }

    [Fact]
    public void JumpHighlight_ChangesBubbleAndBorder_AndIsIdempotent()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel message = CreateMessage("alice", currentUser: "alice");
            Brush brushBefore = message.BubbleBrush;

            message.SetJumpHighlighted(true);
            Assert.True(message.IsJumpHighlighted);
            Assert.NotSame(brushBefore, message.BubbleBrush);
            Assert.Equal(3, message.MessageBorderThickness.Left);

            message.SetJumpHighlighted(true); // no change
            Assert.True(message.IsJumpHighlighted);

            message.SetJumpHighlighted(false);
            Assert.False(message.IsJumpHighlighted);
            Assert.Same(brushBefore, message.BubbleBrush);
        });
    }

    [Fact]
    public void JumpToReply_IsUnavailable_WithoutReplyTarget()
    {
        DispatcherThread.Run(() =>
        {
            MessageViewModel plain = CreateMessage("alice", currentUser: "alice");
            Assert.False(plain.JumpToReplyCommand.CanExecute(null));

            MessageViewModel reply = CreateMessage("alice", currentUser: "alice", replyToMessageId: Guid.NewGuid());
            Assert.True(reply.JumpToReplyCommand.CanExecute(null));
        });
    }

    private static MessageViewModel CreateReactionMessage(Action<string> onReaction)
    {
        ChatMessage message = new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            SenderName = "alice",
            Content = "react here",
            Mentions = new Mentions()
        };

        return new MessageViewModel(
            Guid.NewGuid(),
            message,
            "alice",
            "hostuser",
            _ => { },
            _ => { },
            _ => { },
            _ => { },
            (_, emoji) => onReaction(emoji),
            _ => { });
    }
}
