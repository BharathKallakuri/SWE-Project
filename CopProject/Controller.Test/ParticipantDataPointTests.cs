// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using Controller.Models;

namespace Controller.Test;

public class ParticipantDataPointTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var pt = new ParticipantDataPoint();

        Assert.Equal(0, pt.Seconds);
        Assert.Equal("00:00", pt.TimeLabel);
        Assert.Equal(0, pt.Count);
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var pt = new ParticipantDataPoint {
            Seconds = 125.5,
            TimeLabel = "02:05",
            Count = 8
        };

        Assert.Equal(125.5, pt.Seconds);
        Assert.Equal("02:05", pt.TimeLabel);
        Assert.Equal(8, pt.Count);
    }
}

