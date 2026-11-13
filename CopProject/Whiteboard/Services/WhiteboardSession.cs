// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using Networking;
using Utils;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Server;

namespace Whiteboard.Services;

/// <summary>
/// Encapsulates the network session lifecycle for the whiteboard.
/// Manages server startup, client communicator creation, and user presence notifications.
/// </summary>
public sealed class WhiteboardSession : IDisposable, IUserNotificationHandler
{
    private readonly ICommunicator? _hostClientCommunicator;
    private bool _disposed;

    /// <summary>
    /// Gets the active whiteboard client for this user.
    /// </summary>
    public IWhiteboardClient Client { get; }

    /// <summary>
    /// Gets the server instance if running as host; otherwise null.
    /// </summary>
    public IWhiteboardServer? Server { get; }

    /// <summary>
    /// Raised when a remote participant leaves the meeting.
    /// </summary>
    public event Action<string>? UserLeft;

    /// <summary>
    /// Raised when a participant joins the meeting.
    /// </summary>
    public event Action<ControllerUser>? UserJoined;

    private WhiteboardSession(IWhiteboardClient client, IWhiteboardServer? server, ICommunicator? hostClientCommunicator)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Server = server;
        _hostClientCommunicator = hostClientCommunicator;
    }

    /// <summary>
    /// Starts a new whiteboard session, handling host server creation and dedicated client TCP sockets cleanly.
    /// </summary>
    /// <param name="userId">Current user ID.</param>
    /// <param name="communicator">Session communicator (server communicator on host, client communicator on participant).</param>
    /// <param name="hostIp">Host IP address or hostname.</param>
    /// <param name="port">Host listening port.</param>
    /// <param name="isHost">True if the local user is hosting.</param>
    /// <returns>A new <see cref="WhiteboardSession"/>.</returns>
    public static WhiteboardSession Start(string userId, ICommunicator communicator, string hostIp, int port, bool isHost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(communicator);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostIp);

        IPAddress ip = hostIp.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            ? IPAddress.Loopback
            : (IPAddress.TryParse(hostIp, out IPAddress? parsed) ? parsed : IPAddress.Loopback);

        IPEndPoint endPoint = new(ip, port);

        IWhiteboardServer? server = null;
        ICommunicator? hostClientComm = null;
        IWhiteboardClient client;

        if (isHost)
        {
            server = WhiteboardFactory.GetServerWhiteboard(communicator);
            hostClientComm = CommunicatorFactory.CreateClientCommunicator(endPoint);
            client = WhiteboardFactory.CreateClientWhiteboard(hostClientComm, endPoint, userId);
        }
        else
        {
            client = WhiteboardFactory.CreateClientWhiteboard(communicator, endPoint, userId);
        }

        return new WhiteboardSession(client, server, hostClientComm);
    }

    public void OnUserJoined(ControllerUser user)
    {
        if (user == null)
        {
            return;
        }

        Trace.WriteLine($"[WhiteboardSession] User joined: {user.Username}");
        UserJoined?.Invoke(user);
    }

    public void OnUserLeft(ControllerUser user)
    {
        if (user == null)
        {
            return;
        }

        Trace.WriteLine($"[WhiteboardSession] User left: {user.Username}");
        UserLeft?.Invoke(user.Username);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        (Client as IDisposable)?.Dispose();
        (Server as IDisposable)?.Dispose();
        (_hostClientCommunicator as IDisposable)?.Dispose();
    }
}
