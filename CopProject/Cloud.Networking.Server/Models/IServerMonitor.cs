// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Abstraction over the server monitor so that consumers can be tested with mocks.
/// </summary>
public interface IServerMonitor
{
    event Action? Changed;

    IReadOnlyList<LiveConnection> Connections { get; }

    IReadOnlyList<Message> Messages { get; }

    void Connected(string connectionId);

    void Disconnected(string connectionId);

    void RecordMessage(Message message);
}
