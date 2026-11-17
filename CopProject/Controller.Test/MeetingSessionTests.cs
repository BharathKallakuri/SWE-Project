// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003

using Controller.Models;

namespace Controller.Test;

public class MeetingSessionTests
{
    private static MeetingSession CreateSessionWithoutSyncContext()
    {
        SynchronizationContext? prev = SynchronizationContext.Current;

        try
        {
            SynchronizationContext.SetSynchronizationContext(null);
            return new MeetingSession();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();

        Assert.Empty(session.CurrentUserId);
        Assert.Empty(session.CurrentUserEmail);
        Assert.Null(session.Meeting);
        Assert.False(session.InMeeting);
        Assert.Null(session.MeetingStartTime);
        Assert.Equal(1, session.ParticipantCount);
        Assert.Empty(session.ActiveParticipants);
    }

    [Fact]
    public void BasicProperties_SetAndGet()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();

        session.CurrentUserId = "user-1";
        session.CurrentUserEmail = "user@example.com";
        session.CurrentUserProfilePicture = new byte[] { 1, 2, 3 };
        session.EndReason = "Host left";

        Assert.Equal("user-1", session.CurrentUserId);
        Assert.Equal("user@example.com", session.CurrentUserEmail);
        Assert.Equal(new byte[] { 1, 2, 3 }, session.CurrentUserProfilePicture);
        Assert.Equal("Host left", session.EndReason);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(0, 0)]
    [InlineData(-3, 0)]
    public void ParticipantCount_ClampsToZeroAndRaisesEvent(int input, int expected)
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        int eventValue = -1;

        session.ParticipantCountChanged += count => eventValue = count;
        session.ParticipantCount = input;

        Assert.Equal(expected, session.ParticipantCount);
        Assert.Equal(expected, eventValue);
    }

    [Fact]
    public void ActiveParticipants_Set_UpdatesAndRaisesEvent()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        IReadOnlyList<ParticipantInfo>? received = null;

        session.ParticipantsChanged += list => received = list;

        var participants = new List<ParticipantInfo>
        {
            new() { Username = "sai" },
            new() { Username = "bala" }
        };

        session.ActiveParticipants = participants;

        Assert.Equal(2, session.ActiveParticipants.Count);
        Assert.Same(participants, received);
    }

    [Fact]
    public void StartMeeting_SetsStateAndFiresEvents()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        var info = new MeetingInfo("m1", "8080", true);

        bool started = false;
        bool changed = false;

        session.MeetingStarted += _ => started = true;
        session.Changed += () => changed = true;

        session.StartMeeting(info);

        Assert.Same(info, session.Meeting);
        Assert.True(session.InMeeting);
        Assert.NotNull(session.MeetingStartTime);
        Assert.True(started);
        Assert.True(changed);
    }

    [Fact]
    public void EndMeeting_WhenNotInMeeting_DoesNothing()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        bool ended = false;

        session.MeetingEnded += () => ended = true;

        session.EndMeeting();

        Assert.False(ended);
        Assert.False(session.InMeeting);
    }

    [Fact]
    public void EndMeeting_WhenInMeeting_ResetsStateAndFiresEvents()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();

        session.StartMeeting(new MeetingInfo("m1", "8080", true));
        session.ActiveParticipants = new List<ParticipantInfo>
        {
            new() { Username = "sai" }
        };

        bool ending = false;
        bool ended = false;

        session.MeetingEnding += () => ending = true;
        session.MeetingEnded += () => ended = true;

        session.EndMeeting();

        Assert.True(ending);
        Assert.True(ended);
        Assert.Null(session.Meeting);
        Assert.False(session.InMeeting);
        Assert.Null(session.MeetingStartTime);
        Assert.Empty(session.ActiveParticipants);
    }

    [Fact]
    public void RaiseSecurityAlert_FiresEvent()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        string alert = "";

        session.SecurityAlertReceived += msg => alert = msg;

        session.RaiseSecurityAlert("Suspicious login attempt");

        Assert.Equal("Suspicious login attempt", alert);
    }
}
