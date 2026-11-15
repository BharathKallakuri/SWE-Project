// Author: Rajat
// Roll no.: 112301044

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UI.ViewModels;

namespace Chat.UI.Test;

public class AvatarViewModelTests
{
    // AvatarViewModel is abstract, so exercise it through ParticipantViewModel.
    private static ParticipantViewModel CreateAvatar(string displayName) => new(displayName, "alice", "hostuser");

    [Theory]
    [InlineData("madonna", "M")]
    [InlineData("Ada Lovelace", "AL")]
    [InlineData("Jean Luc Picard", "JP")]
    public void Initials_AreDerivedFromDisplayName(string displayName, string expected)
    {
        DispatcherThread.Run(() =>
        {
            ParticipantViewModel avatar = CreateAvatar(displayName);

            Assert.Equal(expected, avatar.Initials);
        });
    }

    [Fact]
    public void SettingAvatarImage_HidesInitials_AndClearingRestoresThem()
    {
        DispatcherThread.Run(() =>
        {
            ParticipantViewModel avatar = CreateAvatar("Ada Lovelace");
            Assert.IsType<SolidColorBrush>(avatar.AvatarBrush);
            Assert.Equal(Visibility.Visible, avatar.InitialsVisibility);

            avatar.SetAvatarImage(new BitmapImage());

            Assert.Equal(Visibility.Collapsed, avatar.InitialsVisibility);
            Assert.IsType<ImageBrush>(avatar.AvatarBrush);

            avatar.SetAvatarImage(null);

            Assert.Equal(Visibility.Visible, avatar.InitialsVisibility);
            Assert.IsType<SolidColorBrush>(avatar.AvatarBrush);
        });
    }

    [Fact]
    public void AvatarToolTip_CanBeSetAndCleared()
    {
        DispatcherThread.Run(() =>
        {
            ParticipantViewModel avatar = CreateAvatar("Ada Lovelace");
            Assert.Null(avatar.AvatarToolTip);

            avatar.SetAvatarToolTip("Ada Lovelace\nada@example.com");
            Assert.Equal("Ada Lovelace\nada@example.com", avatar.AvatarToolTip);

            avatar.SetAvatarToolTip("   ");
            Assert.Null(avatar.AvatarToolTip);
        });
    }

    [Fact]
    public void AvatarToolTip_SettingSameValueTwice_IsIdempotent()
    {
        DispatcherThread.Run(() =>
        {
            ParticipantViewModel avatar = CreateAvatar("Ada Lovelace");

            avatar.SetAvatarToolTip("Ada\nada@example.com");
            avatar.SetAvatarToolTip("Ada\nada@example.com"); // same value: no change notification

            Assert.Equal("Ada\nada@example.com", avatar.AvatarToolTip);
        });
    }

    [Fact]
    public void DifferentDisplayNames_ProduceStableInitialsBrushColors()
    {
        DispatcherThread.Run(() =>
        {
            ParticipantViewModel first = CreateAvatar("Ada Lovelace");
            ParticipantViewModel second = CreateAvatar("Ada Lovelace");

            SolidColorBrush firstBrush = Assert.IsType<SolidColorBrush>(first.AvatarBrush);
            SolidColorBrush secondBrush = Assert.IsType<SolidColorBrush>(second.AvatarBrush);
            Assert.Equal(firstBrush.Color, secondBrush.Color);
        });
    }
}

public class ParticipantViewModelTests
{
    [Theory]
    [InlineData("alice", "alice", "hostuser", "You")]
    [InlineData("hostuser", "alice", "hostuser", "Host")]
    [InlineData("hostuser", "hostuser", "hostuser", "Host · You")]
    [InlineData("bob", "alice", "hostuser", "")]
    public void ParticipantRoleLabel_ReflectsRelationshipToViewer(
        string username,
        string currentUser,
        string hostUser,
        string expectedLabel)
    {
        DispatcherThread.Run(() =>
        {
            ParticipantViewModel participant = new(username, currentUser, hostUser);

            Assert.Equal(expectedLabel, participant.ParticipantRoleLabel);
        });
    }
}

public class MentionTargetViewModelTests
{
    [Fact]
    public void ToggleCommand_InvokesToggleCallback()
    {
        DispatcherThread.Run(() =>
        {
            int calls = 0;
            MentionTargetViewModel target = new("bob", "bob", "Participant", _ => calls++, _ => { });

            target.ToggleCommand.Execute(null);

            Assert.Equal(1, calls);
            Assert.Equal("+", target.SelectionMark);
        });
    }

    [Fact]
    public void RemoveCommand_InvokesRemoveCallback()
    {
        DispatcherThread.Run(() =>
        {
            int calls = 0;
            MentionTargetViewModel target = new("bob", "bob", "Participant", _ => { }, _ => calls++);

            target.RemoveCommand.Execute(null);

            Assert.Equal(1, calls);
        });
    }

    [Fact]
    public void SetSelected_UpdatesSelectionMark()
    {
        DispatcherThread.Run(() =>
        {
            MentionTargetViewModel target = new("bob", "bob", "Participant", _ => { }, _ => { });
            Assert.Equal("+", target.SelectionMark);

            target.SetSelected(true);
            Assert.True(target.IsSelected);
            Assert.Equal("✓", target.SelectionMark);

            target.SetSelected(true); // idempotent
            Assert.Equal("✓", target.SelectionMark);

            target.SetSelected(false);
            Assert.False(target.IsSelected);
            Assert.Equal("+", target.SelectionMark);
        });
    }
}

public class ReactionViewModelTests
{
    [Fact]
    public void Properties_ReflectDistinctUsers_AndInvokeToggle()
    {
        DispatcherThread.Run(() =>
        {
            List<string> toggled = new();
            ReactionViewModel reaction = new("👍", new[] { "bob", "carol", "bob" }, "alice", toggled.Add);

            Assert.Equal("👍", reaction.Emoji);
            Assert.Equal(2, reaction.Count);
            Assert.Equal("👍 2", reaction.Label);
            Assert.Equal("bob, carol", reaction.Tooltip);
            Assert.False(reaction.IsReactedByCurrentUser);
            Assert.True(reaction.ToggleCommand.CanExecute(null));

            reaction.ToggleCommand.Execute(null);
            Assert.Equal(new[] { "👍" }, toggled);

            reaction.ToggleUser("alice");
            Assert.True(reaction.IsReactedByCurrentUser);
            Assert.Equal(3, reaction.Count);

            reaction.ToggleUser("alice");
            Assert.False(reaction.IsReactedByCurrentUser);

            reaction.ToggleUser("bob");
            Assert.DoesNotContain("bob", reaction.Users);
        });
    }

    [Fact]
    public void ToggleCommand_IsDisabled_WithoutCallback()
    {
        DispatcherThread.Run(() =>
        {
            ReactionViewModel reaction = new("👍", new[] { "bob" }, "alice");

            Assert.False(reaction.ToggleCommand.CanExecute(null));
        });
    }
}
