using System.Diagnostics;


namespace Networking;

public interface ICommunicator
    {
    int ListenPort { get; }

    void AddSubscriber(string id, IMessageListener subscriber);

    void RemoveSubscriber(string id);

    void SendMessage(string IpAdress, int port, string senderId, string message);
}

