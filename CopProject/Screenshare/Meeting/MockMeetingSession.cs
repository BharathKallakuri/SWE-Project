using System.Net;
using Networking;

namespace ScreenShare;

/// <summary>
/// MOCK: stands in for the Controller module until it is ready, for demos and tests.
/// replace it with an adapter over Controller's IControllerService; ScreenShareNode only
/// sees IMeetingSession, so nothing else changes.
///
/// there is no membership tracking here, so ParticipantLeft is never raised. ScreenShare
/// still copes: a sharer who leaves sends StreamStopped, and the host drops a viewer that
/// stops acknowledging frames after ScreenShareOptions.ViewerTimeout.
/// </summary>
public sealed class MockMeetingSession : IMeetingSession, IDisposable
{
    private readonly IDisposable? _connection;
    private int _left;

    /// <summary>
    /// wraps a connection made by the caller, e.g. one wrapped to count bytes
    /// </summary>
    /// <param name="connection">closed when the session is left; null if the caller closes it</param>
    /// <exception cref="ArgumentNullException">an argument is null</exception>
    public MockMeetingSession(MeetingParticipant localUser, IPEndPoint hostEndpoint, ICommunicator communicator, IDisposable? connection)
    {
        ArgumentNullException.ThrowIfNull(localUser);
        ArgumentNullException.ThrowIfNull(hostEndpoint);
        ArgumentNullException.ThrowIfNull(communicator);

        LocalUser = localUser;
        HostEndpoint = hostEndpoint;
        Communicator = communicator;
        _connection = connection;
    }

    public MeetingParticipant LocalUser { get; }

    public IPEndPoint HostEndpoint { get; }

    public ICommunicator Communicator { get; }

    /// <summary>never raised by the mock; see the class summary</summary>
    public event Action<MeetingParticipant>? ParticipantLeft
    {
        add { }
        remove { }
    }

    public event Action? MeetingEnded;

    /// <summary>
    /// starts a meeting on this machine: opens a server connection and makes this
    /// participant the host
    /// </summary>
    /// <exception cref="ArgumentException">the name is empty</exception>
    /// <exception cref="InvalidOperationException">the server could not start listening</exception>
    public static MockMeetingSession Host(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        ICommunicator server = CommunicatorFactory.CreateServerCommunicator();
        if (server.ListenPort == 0)
        {
            (server as IDisposable)?.Dispose();
            throw new InvalidOperationException("the meeting server could not start listening");
        }

        var hostEndpoint = new IPEndPoint(server.ListenIpAddress, server.ListenPort);
        return new MockMeetingSession(NewParticipant(displayName, isHost: true), hostEndpoint, server, server as IDisposable);
    }

    /// <summary>
    /// joins a meeting hosted elsewhere: opens a client connection to the host
    /// </summary>
    /// <exception cref="ArgumentException">the name is empty</exception>
    /// <exception cref="ArgumentNullException">the host endpoint is null</exception>
    public static MockMeetingSession Join(string displayName, IPEndPoint hostEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(hostEndpoint);

        ICommunicator client = CommunicatorFactory.CreateClientCommunicator(hostEndpoint);
        return new MockMeetingSession(NewParticipant(displayName, isHost: false), hostEndpoint, client, client as IDisposable);
    }

    /// <summary>
    /// creates a participant whose id is unique even when two people use the same name
    /// </summary>
    public static MeetingParticipant NewParticipant(string displayName, bool isHost)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        string suffix = Guid.NewGuid().ToString("N")[..6];
        return new MeetingParticipant($"{displayName}-{suffix}", displayName, isHost);
    }

    /// <summary>
    /// leaves the meeting once. MeetingEnded is raised before the connection closes, so
    /// ScreenShare can stop cleanly while the connection still works; the real Controller
    /// adapter must keep this order too.
    /// </summary>
    public void Leave()
    {
        if (Interlocked.Exchange(ref _left, 1) != 0)
        {
            return;
        }

        MeetingEnded?.Invoke();
        _connection?.Dispose();
    }

    public void Dispose() => Leave();
}
