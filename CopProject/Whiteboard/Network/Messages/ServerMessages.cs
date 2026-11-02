// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Network.Messages;

/// <summary>
/// Message sent from the server to one or all clients
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ResUpdate), "res_update")]
[JsonDerivedType(typeof(ResAddItem), "add_item")]
[JsonDerivedType(typeof(ResDeleteItem), "delete_item")]
[JsonDerivedType(typeof(ResUpdateItem), "update_item")]
public abstract record ServerMessage
{
    private ServerMessage() { }

    /// <summary>
    /// Sent when a client requested a complete whiteboard update (unicast).
    /// </summary>
    /// <param name="Whiteboard">Entire whiteboard state from the server</param>
    public sealed record ResUpdate(WhiteboardDto Whiteboard) : ServerMessage;

    /// <summary>
    /// Sent when a server adds an item to its state (broadcast).
    /// </summary>
    /// <param name="Id">GUID of the added item</param>
    /// <param name="Item">Item added</param>
    public sealed record ResAddItem(Guid Id, ItemDto Item) : ServerMessage;

    /// <summary>
    /// Sent when a server deletes an item from its state (broadcast).
    /// </summary>
    /// <param name="Id">GUID of the deleted item</param>
    public sealed record ResDeleteItem(Guid Id) : ServerMessage;

    /// <summary>
    /// Sent when a server updates an item in its state (broadcast).
    /// </summary>
    /// <param name="Id">GUID of the updated item</param>
    /// <param name="Item">Updated item</param>
    public sealed record ResUpdateItem(Guid Id, ItemDto Item) : ServerMessage;
}
