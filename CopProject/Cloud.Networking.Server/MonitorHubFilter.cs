// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR;
using Cloud.Networking.Server.Models;

namespace Cloud.Networking.Server;

/// <summary>
/// This is between Monitor (Models) and Hub (synchronizes Hub calls with UI)
/// Client -> MessageHub -> MonitorHubFilter -> Monitor (as it raises changed event)
/// Monitor -> ViewModel -> View
/// </summary>
public sealed class MonitorHubFilter(ServerMonitor monitor) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        object? result = await next(invocationContext);
        if (result is Message message)
        {
            monitor.RecordMessage(message);
        }

        return result;
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        await next(context);
        // if failure then no connection
        monitor.Connected(context.Context.ConnectionId);
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        try
        {
            await next(context, exception);
        }
        finally
        {
            // no matter success or failure disconnect
            monitor.Disconnected(context.Context.ConnectionId);
        }
    }
}
