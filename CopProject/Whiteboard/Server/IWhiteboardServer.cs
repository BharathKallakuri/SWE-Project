// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Networking;
using Whiteboard.Interfaces;

namespace Whiteboard.Server;

/// <summary>
/// Whiteboard server state manager
/// </summary>
public interface IWhiteboardServer : IMessageListener
{
    /// <summary>
    /// Server whiteboard state
    /// </summary>
    IWhiteboard Canvas { get; }
}
