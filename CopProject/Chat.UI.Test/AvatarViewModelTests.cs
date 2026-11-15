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
}
