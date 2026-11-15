// Author = Bharath
// Roll Number = 112301013

using System.Diagnostics;
using System.Net;
using Networking.TCPCommunication;

namespace Networking;

/// <summary>
/// Factory for creating communicator instances.
/// </summary>
public static class CommunicatorFactory
{
    /// <summary>
    /// Creates a communicator instance.
    /// </summary>
    /// <param name="serverIp">Server endpoint for client mode, or bound endpoint for server mode.</param>
    /// <param name="isServer">True to create a server communicator.</param>
    /// <param name="isClient">True to create a client communicator.</param>
    /// <returns>A new communicator instance, or null if parameters are invalid.</returns>
    [Obsolete("Use CreateClientCommunicator or CreateServerCommunicator instead.")]
    public static ICommunicator? CreateCommunicator(IPEndPoint? serverIp, bool isServer = true, bool isClient = false)
    {
        if (isServer == isClient)
        {
            Trace.TraceWarning("[Networking] CommunicatorFactory.CreateCommunicator: Ambiguity in creation of Communicator, isServer == isClient");
        }
        else if (isServer)
        {
            return new CommunicatorServer(null);
        }
        else
        {
            if (serverIp != null)
            {
                return new CommunicatorClient(serverIp);
            }
            else
            {
                Trace.TraceWarning("[Networking] CommunicatorFactory.CreateCommunicator: Unable to start Client Communicator, Server IP is not provided");
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a client communicator connected to the specified server.
    /// </summary>
    /// <param name="serverIp">The server endpoint to connect to.</param>
    /// <returns>A new client communicator instance.</returns>
    public static ICommunicator CreateClientCommunicator(IPEndPoint serverIp)
    {
        Trace.TraceInformation($"[Networking] CommunicatorFactory: Creating client communicator for server at {serverIp}");
        return new CommunicatorClient(serverIp);
    }

    /// <summary>
    /// Creates a server communicator bound to a free local endpoint.
    /// </summary>
    /// <returns>A new server communicator instance.</returns>
    public static ICommunicator CreateServerCommunicator()
    {
        Trace.TraceInformation("[Networking] CommunicatorFactory: Creating server communicator with dynamic endpoint.");
        return new CommunicatorServer(null);
    }

    /// <summary>
    /// Creates a server communicator bound to the specified endpoint.
    /// </summary>
    /// <param name="serverIp">The local endpoint to bind to, or null to auto-assign.</param>
    /// <returns>A new server communicator instance.</returns>
    public static ICommunicator CreateServerCommunicator(IPEndPoint? serverIp)
    {
        Trace.TraceInformation($"[Networking] CommunicatorFactory: Creating server communicator bound to {serverIp?.ToString() ?? "dynamic endpoint"}");
        return new CommunicatorServer(serverIp);
    }
}
