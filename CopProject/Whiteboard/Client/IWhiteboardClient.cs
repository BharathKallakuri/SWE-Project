// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Networking;
using Whiteboard.Interfaces;

namespace Whiteboard.Client;

/// <summary>
/// Whiteboard client state manager
/// </summary>
public interface IWhiteboardClient : IMessageListener
{
    /// <summary>
    /// Client whiteboard state
    /// </summary>
    public IWhiteboard Canvas { get; }

    /// <summary>
    /// Username of the current client
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Request a complete whiteboard sync to the server
    /// </summary>
    public void RequestSync();

    /// <summary>
    /// Request to add an <see cref="IItem"/> to the server
    /// </summary>
    /// <param name="item">Item to add</param>
    public void AddItem(IItem item);

    /// <summary>
    /// Request to delete an <see cref="IItem"/> to the server
    /// </summary>
    /// <param name="id">Item Id to delete</param>
    public void DeleteItem(Guid id);

    /// <summary>
    /// Request to edit an <see cref="IITem"/> to the server
    /// </summary>
    /// <param name="item">Item to edit</param>
    public void EditItem(IItem item);
}
