// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using Networking;
using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Network.Messages;
using Whiteboard.Network.Serializations;

namespace Whiteboard.Client;

/// <inheritdoc/>
public class WhiteboardClient : IWhiteboardClient
{
    /// <summary>
    /// Client communicator
    /// </summary>
    private readonly ICommunicator _communicator;

    /// <summary>
    /// Endpoint of the server
    /// </summary>
    private readonly IPEndPoint _serverEndpoint;

    public IWhiteboard Canvas { get; } = new WhiteboardCanvas();

    public string UserId { get; }

    public WhiteboardClient(ICommunicator communicator, IPEndPoint serverEndpoint, string userId)
    {
        ArgumentNullException.ThrowIfNull(communicator);
        ArgumentNullException.ThrowIfNull(serverEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        _communicator = communicator;
        _serverEndpoint = serverEndpoint;
        UserId = userId;

        _communicator.AddSubscriber(Constants.WhiteboardModuleId, this);
        RequestSync();
    }

    /// <summary>
    /// Send <see cref="ClientMessage"/> to the server
    /// </summary>
    /// <param name="msg">Client message to send</param>
    private void SendToServer(ClientMessage msg)
    {
        byte[] serialized = new ClientMessageByteSerializer().Serialize(msg);
        _communicator.SendMessage(_serverEndpoint, serialized, Constants.WhiteboardModuleId, priority: Constants.WhiteboardModulePriority);
    }

    public void RequestSync()
    {
        SendToServer(new ClientMessage.ReqUpdate());
    }

    public void AddItem(IItem item)
    {
        SendToServer(new ClientMessage.ReqAddItem(item.Id, ItemMapper.ToDto(item)));
    }

    public void DeleteItem(Guid id)
    {
        SendToServer(new ClientMessage.ReqDeleteItem(id));
    }

    public void EditItem(IItem item)
    {
        SendToServer(new ClientMessage.ReqUpdateItem(item.Id, ItemMapper.ToDto(item)));
    }

    public void OnMessageReceived(byte[] message)
    {
        ServerMessage notification = new ServerMessageByteSerializer().Deserialize(message);

        switch (notification)
        {
            case ServerMessage.ResUpdate(var whiteboardDto):
                Canvas.Items.Clear();
                foreach (ItemDto dto in whiteboardDto.Items)
                {
                    Canvas.AddItem(ItemMapper.FromDto(dto));
                }
                break;

            case ServerMessage.ResAddItem(var id, var itemDto):
                Canvas.AddItem(ItemMapper.FromDto(itemDto));
                break;

            case ServerMessage.ResDeleteItem(var id):
                Canvas.DeleteItem(id);
                break;

            case ServerMessage.ResUpdateItem(var id, var itemDto):
                Canvas.EditItem(ItemMapper.FromDto(itemDto));
                break;
        }
    }
}
