// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using Controller.Models;
using Controller.ViewModels;

namespace Controller.Test;

public class ParticipantInfoTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var p = new ParticipantInfo();

        Assert.Empty(p.Username);
        Assert.Empty(p.Email);
        Assert.False(p.IsHost);
        Assert.Null(p.ProfilePicture);
        Assert.IsAssignableFrom<ObservableObject>(p);
    }

    [Theory]
    [InlineData(true, "Host")]
    [InlineData(false, "Client")]
    public void RoleDisplay_ReflectsIsHost(bool isHost, string expected)
    {
        var p = new ParticipantInfo { IsHost = isHost };
        Assert.Equal(expected, p.RoleDisplay);
    }

    [Theory]
    [InlineData("Sai", true, "Sai (Host)")]
    [InlineData("Bala", false, "Bala")]
    public void DisplayName_ReflectsIsHostAndUsername(string username, bool isHost, string expected)
    {
        var p = new ParticipantInfo { Username = username, IsHost = isHost };
        Assert.Equal(expected, p.DisplayName);
    }

    [Fact]
    public void AvatarImage_WhenNull_ReturnsNull()
    {
        var p = new ParticipantInfo { ProfilePicture = null };
        Assert.Null(p.AvatarImage);
    }

    [Fact]
    public void AvatarImage_WhenEmptyByteArray_ReturnsNull()
    {
        var p = new ParticipantInfo { ProfilePicture = Array.Empty<byte>() };
        Assert.Null(p.AvatarImage);
    }

    [Fact]
    public void AvatarImage_WhenInvalidBytes_CatchesExceptionAndReturnsNull()
    {
        var p = new ParticipantInfo { ProfilePicture = new byte[] { 0x00, 0x11, 0x22, 0x33 } };
        Assert.Null(p.AvatarImage);
    }
}

