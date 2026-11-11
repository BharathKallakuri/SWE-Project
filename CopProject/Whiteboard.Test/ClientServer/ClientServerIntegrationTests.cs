// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Network.Mappers;
using Whiteboard.Server;
using Whiteboard.Test.Mocks.Networking;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.ClientServer;

public class ClientServerIntegrationTests
{
    [Fact]
    public void ClientAddItem_PropagatesToServerAndOtherClients()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        IItem item = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());

        client1.AddItem(item);

        Assert.True(server.Canvas.Items.ContainsKey(item.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item.Id));
    }

    [Fact]
    public void ClientDeleteItem_RemovesFromServerAndOtherClients()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        IItem item = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        client1.AddItem(item);

        Assert.True(server.Canvas.Items.ContainsKey(item.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item.Id));

        client1.DeleteItem(item.Id);

        Assert.False(server.Canvas.Items.ContainsKey(item.Id));
        Assert.False(client1.Canvas.Items.ContainsKey(item.Id));
        Assert.False(client2.Canvas.Items.ContainsKey(item.Id));
    }

    [Fact]
    public void ClientEditItem_UpdatesServerAndOtherClients()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        IItem item = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        client1.AddItem(item);

        item.Orientation = (item.Orientation + 45) % 360;
        client1.EditItem(item);

        Assert.Equal(item.Orientation, server.Canvas.Items[item.Id].Orientation);
        Assert.Equal(item.Orientation, client1.Canvas.Items[item.Id].Orientation);
        Assert.Equal(item.Orientation, client2.Canvas.Items[item.Id].Orientation);
    }

    [Fact]
    public void LateJoiningClient_ReceivesExistingStateOnInitialization()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");

        IItem item1 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item2 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item3 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());

        client1.AddItem(item1);
        client1.AddItem(item2);
        client1.AddItem(item3);

        Assert.Equal(3, server.Canvas.Items.Count);

        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        Assert.Equal(3, client2.Canvas.Items.Count);
        Assert.True(client2.Canvas.Items.ContainsKey(item1.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item2.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item3.Id));
    }

    [Fact]
    public void MultipleClients_ConcurrentAddsAndDeletes_RemainConsistent()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");
        IWhiteboardClient client3 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User3");

        IItem item1 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item2 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item3 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());

        client1.AddItem(item1);
        client2.AddItem(item2);
        client3.AddItem(item3);

        client1.DeleteItem(item1.Id);

        Guid[] expectedIds = [item2.Id, item3.Id];

        Assert.Equal(expectedIds.OrderBy(id => id), server.Canvas.Items.Keys.OrderBy(id => id));
        Assert.Equal(expectedIds.OrderBy(id => id), client1.Canvas.Items.Keys.OrderBy(id => id));
        Assert.Equal(expectedIds.OrderBy(id => id), client2.Canvas.Items.Keys.OrderBy(id => id));
        Assert.Equal(expectedIds.OrderBy(id => id), client3.Canvas.Items.Keys.OrderBy(id => id));
    }
}
