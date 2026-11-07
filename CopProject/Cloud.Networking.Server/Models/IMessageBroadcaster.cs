// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// The one place that turns text into a <see cref="Message"/>, stores it and pushes it to connected clients.
/// </summary>
public interface IMessageBroadcaster
{
    /// <summary>Creates a message, stores it (so offline clients can sync it later) and sends it to all connected clients.</summary>
    /// <param name="senderId">Who sent the message.</param>
    /// <param name="content">The message text (trimmed before being stored).</param>
    /// <returns>The <see cref="Message"/> that was stored and sent.</returns>
    Task<Message> BroadcastAsync(string senderId, string content);
}
