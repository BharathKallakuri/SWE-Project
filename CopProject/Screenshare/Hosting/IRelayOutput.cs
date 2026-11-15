// Author = Sumit
// Roll Number = 112301032

using System.Net;
using ScreenShare;

namespace Screenshare;

internal interface IRealyOutput
{
    void Send(IPEndPoint to, IScreenShareMessage message, byte[] bytes);

    void Broadcast(IScreenShareMessage message, byte[] bytes);

}

