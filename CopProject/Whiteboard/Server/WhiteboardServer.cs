// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Networking;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;

namespace Whiteboard.Server;

/// <summary>
/// Whiteboard server state manager
/// </summary>
public class WhiteboardServer : IMessageListener
{
    /// <summary>
    /// Server communicator
    /// </summary>
    private readonly ICommunicator _communicator;

    /// <summary>
    /// Server whiteboard state
    /// </summary>
    public IWhiteboard Canvas { get; } = new WhiteboardCanvas();

    public WhiteboardServer(ICommunicator communicator)
    {
        ArgumentNullException.ThrowIfNull(communicator);

        _communicator = communicator;

        _communicator.AddSubscriber(Constants.WhiteboardModuleId, this);
    }

    /// <inheritdoc/>
    public void OnMessageReceived(byte[] message)
    {
        ClientMessage request = new ClientMessageByteSerializer().Deserialize(message);

        switch (request)
        {
            case ClientMessage.ReqUpdate:
                // TODO change it to unicast
                var state = new WhiteboardDto([.. Canvas.Items.Values.Select(ItemMapper.ToDto)]);
                Broadcast(new ServerMessage.ResUpdate(state));
                break;

            case ClientMessage.ReqAddItem(var id, var itemDto):
                Canvas.AddItem(ItemMapper.FromDto(itemDto));
                Broadcast(new ServerMessage.ResAddItem(id, itemDto));
                break;

            case ClientMessage.ReqDeleteItem(var id):
                Canvas.DeleteItem(id);
                Broadcast(new ServerMessage.ResDeleteItem(id));
                break;

            case ClientMessage.ReqUpdateItem(var id, var itemDto):
                Canvas.EditItem(ItemMapper.FromDto(itemDto));
                Broadcast(new ServerMessage.ResUpdateItem(id, itemDto));
                break;
        }
    }

    /// <summary>
    /// Broadcast a <see cref="ServerMessage"/> to all clients
    /// </summary>
    /// <param name="message">Server message to broadcast</param>
    private void Broadcast(ServerMessage message)
    {
        byte[] bytes = new ServerMessageByteSerializer().Serialize(message);
        _communicator.BroadcastMessage(bytes, Constants.WhiteboardModuleId, priority: Constants.WhiteboardModulePriority);
    }
}
