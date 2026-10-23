using System.Diagnostics;
using System.Net;
using Networking.TCPCommunication;

namespace Networking;

/// <summary>
/// Factory for creating a communicator.
/// </summary>
public static class CommunicatorFactory
{
    /// <summary>
    /// Creates a communicator.
    /// </summary>
    /// <returns>A new communicator instance</returns>
    public static ICommunicator CreateCommunicator(bool isServer = true, bool isClient = false)
    {
        if (isServer == isClient)
        {
            throw new Exception("[Networking] CommunicatorFactory.CreateCommunicator: Ambiguity in creation of Communicator, isServer == isClient");
        }
        else if (isServer)
        {
            return new CommunicatorServer();
        }
        else
        {
            return CommunicatorClient;
        }
    }
}
