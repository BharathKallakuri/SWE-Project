using ScreenShare;
namespace Screenshare.Tests;

public class MeetingParticipantTests
{
    private readonly MeetingParticipant _hostParticipant = new MeetingParticipant("Sumit", "Sgsuper12373", true);
    private readonly MeetingParticipant _clientParticipant = new MeetingParticipant("Sumit", "clientSumit", false);

    [Fact]
    public void StoreValues()
    {
        Assert.True(_hostParticipant.IsHost);
        Assert.False(_clientParticipant.IsHost);
    }

    [Fact]
    public void RejectEmptyId()
    {
        Assert.Throws<ArgumentNullException>(() => new MeetingParticipant(null!, "sgsuper12373", true));
    }

    [Fact]
    public void RejectEmptyDisplayName()
    {
        Assert.Throws<ArgumentNullException>(() => new MeetingParticipant("Sumit", null!, true));
    }
}
