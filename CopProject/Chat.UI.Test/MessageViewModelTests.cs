// Author: Rajat
// Roll no.: 112301044

using Microsoft.UI.Xaml;
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
