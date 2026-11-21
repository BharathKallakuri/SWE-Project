// Author = Sumit
// Roll Number = 112301032

using System.Net;
using Networking;
using ScreenShare;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// stands in for the meeting when testing ScreenShareNode: a test picks who the local user is
/// and raises ParticipantLeft and MeetingEnded itself.
/// </summary>
internal sealed class FakeMeetingSession : IMeetingSession
{
    public FakeMeetingSession(MeetingParticipant localUser, IPEndPoint hostEndpoint, ICommunicator communicator)
    {
        LocalUser = localUser;
        HostEndpoint = hostEndpoint;
        Communicator = communicator;
    }

    /// <summary>the host of a meeting on the fake network, using the network's server</summary>
    public static FakeMeetingSession Host(FakeNetwork network, string id = "host", string name = "Host")
    {
        return new FakeMeetingSession(new MeetingParticipant(id, name, isHost: true), network.Server.Endpoint, network.Server);
    }

    /// <summary>a client of a meeting on the fake network, with a new connection of its own</summary>
    public static FakeMeetingSession Client(FakeNetwork network, string id, string? name = null)
    {
        return new FakeMeetingSession(new MeetingParticipant(id, name ?? id, isHost: false), network.Server.Endpoint, network.AddClient());
    }

    public MeetingParticipant LocalUser { get; }

    public IPEndPoint HostEndpoint { get; }

    public ICommunicator Communicator { get; }

    public event Action<MeetingParticipant>? ParticipantLeft;

    public event Action? MeetingEnded;

    /// <summary>true while someone listens for ParticipantLeft</summary>
    public bool HasParticipantLeftListeners => ParticipantLeft != null;

    /// <summary>true while someone listens for MeetingEnded</summary>
    public bool HasMeetingEndedListeners => MeetingEnded != null;

    public void RaiseParticipantLeft(MeetingParticipant participant)
    {
        ParticipantLeft?.Invoke(participant);
    }

    public void RaiseMeetingEnded()
    {
        MeetingEnded?.Invoke();
    }
}
