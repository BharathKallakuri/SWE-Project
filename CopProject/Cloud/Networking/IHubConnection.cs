// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.SignalR.Client;

namespace Cloud.Networking;

/// <summary>
/// Mockable Interface around the <see cref="HubConnection"/> class.
/// Also, <see cref="HubConnectionExtensions"/> for <c>On</c> and <c>InvokeAsync</c>  function signatures.
/// </summary>
/// 
/// <remarks>
/// - HubConnection does not expose an interface.
/// - SignalR extension methods such as <c>On</c> and <c>InvokeAsync</c> cannot be mocked directly by Moq.
/// </remarks>
/// 
/// Reference:
/// https://stackoverflow.com/questions/65009412/how-to-mock-a-wrapped-microsoft-asp-net-core-client-signalr-hubconnection-class
public interface IHubConnection : IAsyncDisposable
{
    HubConnectionState State { get; }

    event Func<Exception?, Task>? Reconnecting;
    event Func<string?, Task>? Reconnected;
    event Func<Exception?, Task>? Closed;

    IDisposable On<T>(string methodName, Action<T> handler);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task InvokeAsync(string methodName, object? arg, CancellationToken cancellationToken = default);
}
