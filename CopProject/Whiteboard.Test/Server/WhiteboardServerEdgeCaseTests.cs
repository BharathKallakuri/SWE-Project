/// LICENSE : MIT

using Networking;
using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;
using Whiteboard.Server;
using Whiteboard.Test.Mocks.Networking;

namespace Whiteboard.Test.Server;

public class WhiteboardServerEdgeCaseTests
{
    [Fact]
    public void OnMessageReceived_IgnoresNullAndUnauthenticatedRequests()
    {
        ICommunicator serverComm = new MockCommunicatorFactory().GetServerCommunicator();
        WhiteboardServer server = new(serverComm);
        ClientMessageByteSerializer serializer = new();

        server.OnMessageReceived(serializer.Serialize(null!));
        server.OnMessageReceived(serializer.Serialize(new ClientMessage.ReqUpdate { UserId = "   " }));

        Assert.NotNull(server);
    }
}
