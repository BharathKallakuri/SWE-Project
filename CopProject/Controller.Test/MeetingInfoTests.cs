// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using Controller.Models;
using Xunit;

namespace Controller.Test;

public class MeetingInfoTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var info = new MeetingInfo("meet-123", "8080", true);

        Assert.Equal("meet-123", info.Id);
        Assert.Equal("8080", info.Port);
        Assert.True(info.IsHost);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new MeetingInfo("m1", "5000", true);
        var b = new MeetingInfo("m1", "5000", true);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new MeetingInfo("m1", "5000", true);
        var b = new MeetingInfo("m2", "5000", true);
        var c = new MeetingInfo("m1", "5001", true);
        var d = new MeetingInfo("m1", "5000", false);

        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
        Assert.NotEqual(a, d);
    }

    [Fact]
    public void Record_WithExpression_CopiesAndMutates()
    {
        var original = new MeetingInfo("m1", "5000", true);
        MeetingInfo updated = original with { IsHost = false };

        Assert.Equal("m1", updated.Id);
        Assert.Equal("5000", updated.Port);
        Assert.False(updated.IsHost);
    }

    [Fact]
    public void ToString_ReturnsFormattedRecordString()
    {
        var info = new MeetingInfo("test-id", "9000", false);
        string str = info.ToString();

        Assert.Contains("test-id", str);
        Assert.Contains("9000", str);
        Assert.Contains("False", str);
    }
}

