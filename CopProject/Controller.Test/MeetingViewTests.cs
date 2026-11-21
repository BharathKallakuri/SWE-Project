// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System.Reflection;
using Controller.Views;

namespace Controller.Test;

public class MeetingViewTests
{
    [Fact]
    public void ViewModelProperty_MetadataIsCorrect()
    {
        Type type = typeof(MeetingView);
        PropertyInfo? prop = type.GetProperty(nameof(MeetingView.ViewModel));

        Assert.NotNull(prop);
        Assert.True(prop.CanRead);
        Assert.True(prop.CanWrite);
    }

    [Fact]
    public void ViewMethods_EventHandlers_Exist()
    {
        Type type = typeof(MeetingView);

        Assert.NotNull(type.GetMethod("GraphCard_Tapped", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.NotNull(type.GetMethod("MaximizeGraph_Click", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.NotNull(type.GetMethod("AuditCard_Tapped", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.NotNull(type.GetMethod("MaximizeAudit_Click", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.NotNull(type.GetMethod("CloseMaximized_Click", BindingFlags.NonPublic | BindingFlags.Instance));
    }
}
