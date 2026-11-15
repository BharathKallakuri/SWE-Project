// Author = Sumit
// Roll Number = 112301032

namespace ScreenShare;

/// <summary>
/// a timer made by an IUiScheduler. Start while running restarts the wait.
/// Dispose stops it and lets go of its tick.
/// </summary>
internal interface IUiTimer : IDisposable
{
    void Start();

    void Stop();
}
