// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Cloud.Networking.Server.ViewModels;

public sealed record ConnectionItem(
    string ConnectionId,
    string ConnectedAt
);

public sealed record MessageItem(
    string Id,
    string Time,
    string From,
    string Content,
    bool IsFromServer
);
