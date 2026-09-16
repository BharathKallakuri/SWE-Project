using System.Diagnostics;

namespace Networking
{
    public static class CommunicatorFactory
    {
        public static ICommunicator CreateCommunicator()
        {
            ICommunicator communicator = new UdpCommunicator();
            Trace.TraceInformation($"Starting UDP Communicator in port {communicator.ListenPort}");
            return communicator;
        }
    }
}
