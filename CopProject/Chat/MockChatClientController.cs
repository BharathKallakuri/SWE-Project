using System.Net;
using System.Text.Json;
using Networking;

namespace Chat;

/// <summary>
/// Sends participant lifecycle notices to the host-side mock controller.
/// </summary>
public sealed class MockChatClientController : IDisposable
{
    private readonly string _username;
    private readonly ICommunicator _communicator;
    private readonly IPEndPoint _serverAddress;
    private bool _joined;
    private bool _disposed;

    public MockChatClientController(
        string username,
        ICommunicator communicator,
        IPEndPoint serverAddress) { ArgumentException.ThrowIfNullOrWhiteSpace(username); ArgumentNullException.ThrowIfNull(communicator);
        ArgumentNullException.ThrowIfNull(serverAddress);

        _username = username;
        _communicator = communicator;
        _serverAddress = serverAddress;
    }

    public void JoinMeeting()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_joined)
        {
            return;
        }

        IPAddress localAddress = _communicator.ListenIpAddress;
        if (localAddress.IsIPv4MappedToIPv6)
        {
            localAddress = localAddress.MapToIPv4();
        }

        int localPort = _communicator.ListenPort;
        if (localAddress.Equals(IPAddress.Any) || localPort is < 1 or > 65535)
        {
            throw new InvalidOperationException("The client communicator has no usable local endpoint.");
        }

        MockChatControllerMessage request = new(
            MockChatControllerMessage.Join,
            _username,
            localAddress.ToString(),
            localPort);
        Send(request);
        _joined = true;
    }

    public void LeaveMeeting()
    {
        if (!_joined)
        {
            return;
        }

        Send(new MockChatControllerMessage(
            MockChatControllerMessage.Leave,
            _username,
            null,
            null));
        _joined = false;
        Thread.Sleep(150);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        LeaveMeeting();
        _disposed = true;
    }

    private void Send(MockChatControllerMessage request)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request);
        _communicator.SendMessage(
            _serverAddress,
            payload,
            MockChatController.ModuleId,
            priority: 1);
    }
}

internal sealed record MockChatControllerMessage(
    string Type,
    string Username,
    string? IpAddress,
    int? Port)
{
    public const string Join = "join";
    public const string Leave = "leave";
}
