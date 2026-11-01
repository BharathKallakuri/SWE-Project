// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Network.Messages;

/// <summary>
/// Message sent from the server to a client for acknowledgement
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AckUpdate), "ack_update")]
[JsonDerivedType(typeof(AckAddItem), "ack_add_item")]
[JsonDerivedType(typeof(AckDeleteItem), "ack_delete_item")]
[JsonDerivedType(typeof(AckUpdateItem), "ack_update_item")]
public abstract record ServerAckMessage
{
    private ServerAckMessage() { }

    /// <summary>
    /// Sent when a client requested a complete whiteboard update.
    /// </summary>
    /// <param name="Whiteboard">Entire whiteboard state from the server</param>
    public sealed record AckUpdate(WhiteboardDto Whiteboard) : ServerAckMessage;

    /// <summary>
    /// Sent when a client requested approval for an item addition.
    /// </summary>
    /// <param name="Id">GUID of the added item</param>
    public sealed record AckAddItem(Guid Id) : ServerAckMessage;

    /// <summary>
    /// Sent when a client requested approval for an item deletion.
    /// </summary>
    /// <param name="Id">GUID of the deleted item</param>
    public sealed record AckDeleteItem(Guid Id) : ServerAckMessage;

    /// <summary>
    /// Sent when a client requested approval for an item edit.
    /// </summary>
    /// <param name="Id">GUID of the item updated</param>
    public sealed record AckUpdateItem(Guid Id) : ServerAckMessage;
}

/// <summary>
/// Message sent from the server to all clients (broadcast) upon state change
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BrdAddItem), "brd_add_item")]
[JsonDerivedType(typeof(BrdDeleteItem), "brd_delete_item")]
[JsonDerivedType(typeof(BrdUpdateItem), "brd_update_item")]
public abstract record ServerBrdMessage
{
    private ServerBrdMessage() { }

    /// <summary>
    /// Sent when a server adds an item to its state.
    /// </summary>
    /// <param name="Id">GUID of the added item</param>
    /// <param name="Item">Item added</param>
    public sealed record BrdAddItem(Guid Id, ItemDto Item) : ServerBrdMessage;

    /// <summary>
    /// Sent when a server deletes an item from its state.
    /// </summary>
    /// <param name="Id">GUID of the deleted item</param>
    public sealed record BrdDeleteItem(Guid Id) : ServerBrdMessage;

    /// <summary>
    /// Sent when a server updates an item in its state.
    /// </summary>
    /// <param name="Id">GUID of the updated item</param>
    /// <param name="Item">Updated item</param>
    public sealed record BrdUpdateItem(Guid Id, ItemDto Item) : ServerBrdMessage;
}
