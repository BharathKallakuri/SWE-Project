// Author = Simeon Kandan Sonar
// Roll Number = 112301031

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cloud.Networking.Server.Models;

/// <summary>
/// Keeps the Monitor's connection list in step with the Hub's connection lifecycle.
/// Client -> MessageHub -> MonitorHubFilter -> Monitor (as it raises changed event)
/// Monitor -> ViewModel -> View
/// Messages do not go through here: the Monitor hears about them from the message store.
/// </summary>
/// 
/// <remarks>
/// Reference: https://learn.microsoft.com/en-us/aspnet/core/signalr/hub-filters?view=aspnetcore-10.0
/// 
/// </remarks>
public sealed class MonitorHubFilter : IHubFilter
{
    private readonly IServerMonitor _monitor;
    private readonly ILogger<MonitorHubFilter> _logger;

    /// <summary>Initializes the filter.</summary>
    /// <param name="monitor">The monitor that keeps track of the live connections</param>
    /// <param name="logger">Where connection lifecycle and hub call information is logged (optional)</param>
    public MonitorHubFilter(IServerMonitor monitor, ILogger<MonitorHubFilter>? logger = null)
    {
        _monitor = monitor;
        _logger = logger ?? NullLogger<MonitorHubFilter>.Instance;
    }

    /// <summary>
    /// Adds a connection only after the connection pipeline succeeds (prevents "false active connections")
    /// </summary>
    /// <param name="context">Context about the connecting client.</param>
    /// <param name="next">Delegate that invokes the next operation in the connection pipeline.</param>
    /// <returns>A task representing the asynchronous connection operation</returns>
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        string connectionId = context.Context.ConnectionId;
        string? remoteAddress = context.Context.Features?.Get<IHttpContextFeature>()?.HttpContext?.Connection.RemoteIpAddress?.ToString();

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // if failure then no connection
            _logger.LogError(ex, "Connection {ConnectionId} from {RemoteAddress} failed while connecting", connectionId, remoteAddress);
            throw;
        }

        _monitor.Connected(connectionId);
        _logger.LogInformation("Client connected: {ConnectionId} from {RemoteAddress}", connectionId, remoteAddress);
    }

    /// <summary>
    /// Removes a connection even if the disconnection pipeline fails (prevents "stale entries")
    /// </summary>
    /// <param name="context">Context about the disconnecting client.</param>
    /// <param name="exception">Exception associated with the disconnection</param>
    /// <param name="next">Delegate that invokes the next operation in the disconnection pipeline.</param>
    /// <returns>A task representing the asynchronous disconnection operation.</returns>
    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        string connectionId = context.Context.ConnectionId;

        if (exception is null)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogWarning(exception, "Client disconnected with an error: {ConnectionId}", connectionId);
        }

        try
        {
            await next(context, exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection {ConnectionId} failed while disconnecting", connectionId);
            throw;
        }
        finally
        {
            // no matter success or failure disconnect
            _monitor.Disconnected(connectionId);
        }
    }

    /// <summary>
    /// Logs every hub method call, how long it took and how it ended.
    /// A <see cref="HubException"/> is the client's fault (bad input) so it is a warning, anything else is an error.
    /// The exception is always rethrown, so SignalR still reports it to the caller as before.
    /// </summary>
    /// <param name="invocationContext">Context about the hub method being called.</param>
    /// <param name="next">Delegate that invokes the hub method.</param>
    /// <returns>The result of the hub method</returns>
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        string method = invocationContext.HubMethodName;
        string connectionId = invocationContext.Context.ConnectionId;
        long start = Stopwatch.GetTimestamp();

        _logger.LogDebug("Hub call {Method} started by {ConnectionId}", method, connectionId);

        try
        {
            object? result = await next(invocationContext);
            _logger.LogDebug("Hub call {Method} by {ConnectionId} completed in {ElapsedMs} ms", method, connectionId, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            return result;
        }
        catch (HubException ex)
        {
            _logger.LogWarning(ex, "Hub call {Method} by {ConnectionId} was rejected after {ElapsedMs} ms: {Reason}", method, connectionId, Stopwatch.GetElapsedTime(start).TotalMilliseconds, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hub call {Method} by {ConnectionId} failed after {ElapsedMs} ms", method, connectionId, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            throw;
        }
    }
}
