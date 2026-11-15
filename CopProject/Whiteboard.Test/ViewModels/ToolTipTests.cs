/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.ViewModels;

namespace Whiteboard.Test.ViewModels;

public class ToolTipTests
{
    [Fact]
    public void ToolTip_Properties_And_Equality()
    {
        DateTime now = DateTime.UtcNow;
        ToolTip tip1 = new("UserA", now);
        ToolTip tip2 = new("UserA", now);
        ToolTip tip3 = tip1 with { CreatedBy = "UserB" };

        Assert.Equal("UserA", tip1.CreatedBy);
        Assert.Equal(now, tip1.CreatedDateTime);
        Assert.Equal(tip1, tip2);
        Assert.NotEqual(tip1, tip3);
        Assert.Equal("UserB", tip3.CreatedBy);
    }
}
