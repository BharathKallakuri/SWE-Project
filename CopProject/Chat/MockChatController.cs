using System.Net;
using System.Text.Json;
using Networking;

namespace Chat;

/// <summary>
/// Provides in-memory participant events and maps usernames to network endpoints.
/// </summary>
public sealed class MockChatController : IChatController, IMessageListener, IDisposable
{
    public const int ModuleId = 1;

    private readonly Dictionary<string, IPEndPoint> _participantEndPoints = new(StringComparer.Ordinal);
    private readonly object _syncRoot = new();
    private readonly ICommunicator _communicator;
    private bool _disposed;

    /// <inheritdoc />
    public string HostName { get; }

    /// <inheritdoc />
    public event Action<string>? ParticipantAdded;

    /// <inheritdoc />
    public event Action<string>? ParticipantRemoved;

    public MockChatController(string hostName, ICommunicator communicator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);
        ArgumentNullException.ThrowIfNull(communicator);
        HostName = hostName;
        _communicator = communicator;
        _communicator.AddSubscriber(ModuleId, this);
    }

    /// <inheritdoc />
    public IPEndPoint GetParticipantEndPoint(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        lock (_syncRoot)
        {
            return _participantEndPoints.TryGetValue(username, out IPEndPoint? endPoint)
                ? endPoint
                : throw new KeyNotFoundException($"No network endpoint is registered for participant '{username}'.");
        }
    }

    /// <summary>
    /// Receives participant lifecycle notices from the mock client controller.
    /// </summary>
    /// <param name="message">The serialized controller message.</param>
    public void OnMessageReceived(byte[] message)
    {
        ArgumentNullException.ThrowIfNull(message);
        MockChatControllerMessage request = JsonSerializer.Deserialize<MockChatControllerMessage>(message)
            ?? throw new InvalidDataException("The received controller message was empty.");

        switch (request.Type)
        {
            case MockChatControllerMessage.Join:
                ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);
                if (!IPAddress.TryParse(request.IpAddress, out IPAddress? ipAddress)
                    || request.Port is not int port
                    || port is < 1 or > 65535)
                {
                    throw new InvalidDataException("The participant join message contained an invalid endpoint.");
                }

                AddParticipant(request.Username, new IPEndPoint(ipAddress, port));
                break;

            case MockChatControllerMessage.Leave:
                ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);
                RemoveParticipant(request.Username);
                break;

            default:
                throw new InvalidDataException($"Unsupported controller message type: {request.Type}.");
        }
    }

    /// <summary>
    /// Stores the participant endpoint and raises the participant-joined event.
    /// </summary>
    /// <param name="username">The username of the participant joining.</param>
    /// <param name="endPoint">The participant's IP address and port.</param>
    public void AddParticipant(string username, IPEndPoint endPoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(endPoint);
        lock (_syncRoot)
        {
            _participantEndPoints[username] = endPoint;
        }

        ParticipantAdded?.Invoke(username);
    }

    /// <summary>
    /// Raises the participant-left event.
    /// </summary>
    /// <param name="username">The username of the participant leaving.</param>
    public void RemoveParticipant(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        lock (_syncRoot)
        {
            _participantEndPoints.Remove(username);
        }

        ParticipantRemoved?.Invoke(username);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _communicator.RemoveSubscriber(ModuleId);
        _disposed = true;
    }
}
