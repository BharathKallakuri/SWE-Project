// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using Controller.Models;

namespace Controller.Test;

public class SecurityAuditEventTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var evt = new SecurityAuditEvent();

        Assert.False(string.IsNullOrWhiteSpace(evt.Time));
        Assert.Empty(evt.User);
        Assert.Empty(evt.Activity);
        Assert.Equal("Success", evt.Status);
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var evt = new SecurityAuditEvent {
            Time = "12:30:00",
            User = "Host",
            Activity = "Joined meeting",
            Status = "Info"
        };

        Assert.Equal("12:30:00", evt.Time);
        Assert.Equal("Host", evt.User);
        Assert.Equal("Joined meeting", evt.Activity);
        Assert.Equal("Info", evt.Status);
    }
}

