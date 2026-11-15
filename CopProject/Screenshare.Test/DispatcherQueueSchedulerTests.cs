// Author = Sumit
// Roll Number = 112301032

using ScreenShare;

namespace Screenshare.Tests;

// only the argument check can run here: creating a WinUI DispatcherQueue needs the Windows
// App SDK runtime, which the test process can't load ("Class not registered"), so Post and
// the timers are only exercised in the running app
public class DispatcherQueueSchedulerTests
{
    [Fact]
    public void Constructor_NullQueue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DispatcherQueueScheduler(null!));
    }
}
