// Author : Saideep.
// Roll No : 112301018.
using System.Net;
using System.Net.Sockets;

namespace Controller;

///<summary>
/// provides utility methods for resolving local machine's network IP address.
///</summary>
public static class NetworkUtils
{   
    ///<summary>
    ///Returns the local machine's IP address on the network.
    ///</summary>
    
       
    public static string GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 80);
            return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
        }
        catch
        {
            // Fallback for machines with no network access
            return "127.0.0.1";
        }
    }
}
