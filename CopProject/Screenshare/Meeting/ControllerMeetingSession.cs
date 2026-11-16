// Author = Sumit
// Roll Number = 112301032

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Controller;
using Controller.Models;
using Networking;
using Utils;

namespace ScreenShare;

/// <summary>
/// the meeting as ScreenShare sees it, built from the Controller module for one meeting.
///
/// a participant's id is the Controller's UserKey (the user's gmail): the client and the host
/// build it from the same values, so the host can match a leaving user to their stream.
/// create it after the meeting started and dispose it when the meeting ends.
/// </summary>
public sealed class ControllerMeetingSession : IMeetingSession, IUserNotificationHandler, IDisposable
{
    /// <summary>names longer than this are cut so they always fit in a packet</summary>
    internal const int MaxDisplayNameLength = 64;

    private const string FallbackDisplayName = "Participant";

    private readonly IControllerService _controller;
    private readonly MeetingSession _session;
    private int _ended;

    /// <param name="controller">the controller of the running meeting</param>
    /// <param name="session">the UI's meeting session; its MeetingEnding ends ScreenShare</param>
    /// <param name="meeting">the meeting that started; gives the host's address to clients</param>
    /// <exception cref="ArgumentNullException">an argument is null</exception>
    /// <exception cref="InvalidOperationException">the controller has no user or connection</exception>
    /// <exception cref="FormatException">the meeting's address or port is not valid</exception>
    public ControllerMeetingSession(IControllerService controller, MeetingSession session, MeetingInfo meeting)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(meeting);

        ControllerUser user = controller.CurrentUser
            ?? throw new InvalidOperationException("the controller has no current user");
        Communicator = controller.Communicator
            ?? throw new InvalidOperationException("the controller has no meeting connection");

        LocalUser = ToParticipant(user);

        // the host never sends to itself over Networking, so its own endpoint is only a placeholder
        HostEndpoint = LocalUser.IsHost
            ? new IPEndPoint(Communicator.ListenIpAddress, Communicator.ListenPort)
            : ParseHostEndpoint(meeting.Id, meeting.Port);

        _controller = controller;
        _session = session;
        _session.MeetingEnding += OnMeetingEnding;
        _controller.RegisterNotificationHandler(this);

        Trace.TraceInformation(
            "[Screenshare] ControllerMeetingSession: started for {0} ({1}) as the {2}; host at {3}.",
            LocalUser.DisplayName,
            LocalUser.Id,
            LocalUser.IsHost ? "host" : "client",
            HostEndpoint);
    }

    public MeetingParticipant LocalUser { get; }

    public IPEndPoint HostEndpoint { get; }

    public ICommunicator Communicator { get; }

    public event Action<MeetingParticipant>? ParticipantLeft;

    public event Action? MeetingEnded;

    /// <summary>converts a Controller user to the participant ScreenShare knows them as</summary>
    /// <exception cref="ArgumentNullException">the user is null</exception>
    public static MeetingParticipant ToParticipant(ControllerUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new MeetingParticipant(ParticipantId(user), DisplayName(user), user.IsHost);
    }

    /// <summary>
    /// gets the user's UserKey, or a hash of it when it is too long for a packet
    /// </summary>
    internal static string ParticipantId(ControllerUser user)
    {
        string key = user.UserKey;
        byte[] bytes = Encoding.UTF8.GetBytes(key);
        if (bytes.Length <= StreamLayers.MaxParticipantIdBytes)
        {
            return key;
        }

        return "user-" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    /// <summary>gets the user's name, else their email, cut to MaxDisplayNameLength</summary>
    internal static string DisplayName(ControllerUser user)
    {
        string name;

        if (!string.IsNullOrWhiteSpace(user.Username))
        {
            name = user.Username.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(user.Gmail))
        {
            name = user.Gmail.Trim();
        }
        else
        {
            name = FallbackDisplayName;
        }

        if (name.Length > MaxDisplayNameLength)
        {
            name = name[..MaxDisplayNameLength];
        }

        return name;
    }

    /// <summary>
    /// parses the meeting id and port the user joined with. like the Controller, "localhost"
    /// means 127.0.0.1.
    /// </summary>
    /// <exception cref="FormatException">the address or port is not valid</exception>
    internal static IPEndPoint ParseHostEndpoint(string address, string port)
    {
        string host = address.Trim();

        IPAddress ip;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            ip = IPAddress.Loopback;
        }
        else if (!IPAddress.TryParse(host, out ip!))
        {
            throw new FormatException($"'{address}' is not a valid meeting address");
        }

        if (!int.TryParse(port.Trim(), out int number))
        {
            throw new FormatException($"'{port}' is not a valid meeting port");
        }

        return number <= 0 || number > IPEndPoint.MaxPort
            ? throw new FormatException($"'{port}' is not a valid meeting port")
            : new IPEndPoint(ip, number);
    }

    /// <summary>ScreenShare learns who shares from its own messages, so joins are not needed</summary>
    public void OnUserJoined(ControllerUser user)
    {
        Trace.TraceInformation("[Screenshare] ControllerMeetingSession.OnUserJoined: a participant joined; nothing to do until they share.");
    }

    /// <summary>called by the Controller on its own thread</summary>
    public void OnUserLeft(ControllerUser user)
    {
        if (user == null || Volatile.Read(ref _ended) != 0)
        {
            return;
        }

        try
        {
            MeetingParticipant participant = ToParticipant(user);
            Trace.TraceInformation(
                "[Screenshare] ControllerMeetingSession.OnUserLeft: {0} ({1}) left.",
                participant.DisplayName,
                participant.Id);
            ParticipantLeft?.Invoke(participant);
        }
        catch (Exception e)
        {
            // never throw into the Controller's thread
            Trace.TraceError("[Screenshare] ControllerMeetingSession.OnUserLeft: could not handle a participant leaving: {0}", e.Message);
        }
    }

    /// <summary>stops listening to the Controller without raising MeetingEnded</summary>
    public void Dispose()
    {
        End(raiseMeetingEnded: false);
    }

    private void OnMeetingEnding()
    {
        End(raiseMeetingEnded: true);
    }

    private void End(bool raiseMeetingEnded)
    {
        if (Interlocked.Exchange(ref _ended, 1) != 0)
        {
            return;
        }

        Trace.TraceInformation(
            "[Screenshare] ControllerMeetingSession.End: {0}.",
            raiseMeetingEnded ? "the meeting is ending" : "disposed");
        _session.MeetingEnding -= OnMeetingEnding;
        try
        {
            _controller.UnregisterNotificationHandler(this);
        }
        catch (Exception e)
        {
            // the controller may already be disposed
            Trace.TraceWarning("[Screenshare] ControllerMeetingSession.End: could not unregister from the controller: {0}", e.Message);
        }

        if (raiseMeetingEnded)
        {
            MeetingEnded?.Invoke();
        }
    }
}
