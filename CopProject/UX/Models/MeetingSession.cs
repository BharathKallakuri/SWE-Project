using System;
using System.Collections.Generic;
using System.Threading;
using SecureMeet.Shared.Models;
using SecureMeet.Shared.Modules;

namespace UX.Models;

public sealed class MeetingSession : IMeetingSession
{
    readonly AuthService auth;
    readonly SynchronizationContext? uiThread = SynchronizationContext.Current;
    int participantCount = 1;

    public MeetingSession(AuthService auth) { this.auth = auth; }

    public UserProfile? CurrentUser { get; private set; }
    public IReadOnlyList<UserProfile> Users => auth.Users;
    public MeetingInfo? Meeting { get; private set; }
    public bool InMeeting => Meeting != null;

    public int ParticipantCount
    {
        get => participantCount;
        set
        {
            participantCount = Math.Max(0, value);
            if (uiThread != null) uiThread.Post(_ => ParticipantCountChanged?.Invoke(participantCount), null);
            else ParticipantCountChanged?.Invoke(participantCount);
        }
    }

    private IReadOnlyList<ParticipantInfo> _activeParticipants = Array.Empty<ParticipantInfo>();
    public IReadOnlyList<ParticipantInfo> ActiveParticipants
    {
        get => _activeParticipants;
        set
        {
            _activeParticipants = value ?? Array.Empty<ParticipantInfo>();
            if (uiThread != null) uiThread.Post(_ => ParticipantsChanged?.Invoke(_activeParticipants), null);
            else ParticipantsChanged?.Invoke(_activeParticipants);
        }
    }

    public event Action<UserProfile>? UserSignedIn;
    public event Action? UserSignedOut;
    public event Action<UserProfile>? ProfileChanged;
    public event Action<MeetingInfo>? MeetingStarted;
    public event Action? MeetingEnded;
    public event Action<PanelKind, bool>? PanelVisibilityChanged;
    public event Action<int>? ParticipantCountChanged;
    public event Action<IReadOnlyList<ParticipantInfo>>? ParticipantsChanged;

    bool incidentsOpen;

    public bool IncidentsOpen
    {
        get => incidentsOpen;
        set { incidentsOpen = value; Changed?.Invoke(); }
    }

    public event Action? Changed;

    public void SignIn(UserProfile user)
    {
        CurrentUser = user;
        incidentsOpen = false;
        Changed?.Invoke();
        UserSignedIn?.Invoke(user);
    }

    public void SignOut()
    {
        EndMeeting();
        CurrentUser = null;
        incidentsOpen = false;
        Changed?.Invoke();
        UserSignedOut?.Invoke();
    }

    public void UpdateProfile(UserProfile user)
    {
        CurrentUser = user;
        ProfileChanged?.Invoke(user);
    }

    public void StartMeeting(MeetingInfo info)
    {
        Meeting = info;
        participantCount = 1;
        Changed?.Invoke();
        MeetingStarted?.Invoke(info);
    }

    public void EndMeeting()
    {
        if (Meeting == null) return;
        Meeting = null;
        incidentsOpen = false;
        _activeParticipants = Array.Empty<ParticipantInfo>();
        ParticipantsChanged?.Invoke(_activeParticipants);
        MeetingEnded?.Invoke();
        Changed?.Invoke();
    }

    public void NotifyPanelVisibility(PanelKind panel, bool visible)
        => PanelVisibilityChanged?.Invoke(panel, visible);
}