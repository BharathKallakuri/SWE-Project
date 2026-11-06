using System.Net;
using ScreenShare;

namespace Screenshare;

internal interface IRealyOutput
{
    void Send(IPEndPoint to , IScreenShareMessage message, byte[] bytes);

    void Brodcast(IScreenShareMessage message, byte[] bytes);

}

