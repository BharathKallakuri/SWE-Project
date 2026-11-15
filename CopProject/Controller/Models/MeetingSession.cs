// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Controller.Models;
// class for managing the current meeting state, user details, and participant information.
public sealed class MeetingSession
{
    // Captures the UI thread context to safely notify UI components
    readonly SynchronizationContext? uiThread = SynchronizationContext.Current;
    int participantCount = 1;

    public MeetingSession() { }

    public string CurrentUserId { get; set; } = string.Empty;
    public string CurrentUserEmail { get; set; } = string.Empty;
    public byte[]? CurrentUserProfilePicture { get; set; }
    
    // Stores meeting details and tracks the meeting's current status and duration
    public MeetingInfo? Meeting { get; private set; }
    public bool InMeeting => Meeting != null;
    public DateTime? MeetingStartTime { get; private set; }
    public string? EndReason { get; set; }

    // Stores meeting details and tracks the meeting's current status and duration
    public int ParticipantCount
    {
        get => participantCount;
        set
        {
            participantCount = Math.Max(0, value);
            Trace.TraceInformation($"[MeetingSession] ParticipantCount updated to {participantCount}.");
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
            Trace.TraceInformation($"[MeetingSession] ActiveParticipants updated: {_activeParticipants.Count} participant(s).");
            if (uiThread != null) uiThread.Post(_ => ParticipantsChanged?.Invoke(_activeParticipants), null);
            else ParticipantsChanged?.Invoke(_activeParticipants);
        }
    }

    public event Action<MeetingInfo>? MeetingStarted;
    public event Action? MeetingEnding;
    public event Action? MeetingEnded;
    public event Action<int>? ParticipantCountChanged;
    public event Action<IReadOnlyList<ParticipantInfo>>? ParticipantsChanged;
    public event Action<string>? SecurityAlertReceived;
    public event Action? Changed;

    // Initializes a meeting, resets the participant count, and raises meeting-start notifications.
    public void StartMeeting(MeetingInfo info)
    {
        Trace.TraceInformation($"[MeetingSession] Starting meeting {info.Id} on port {info.Port} (IsHost: {info.IsHost}).");
        Meeting = info;
        participantCount = 1;
        MeetingStartTime = DateTime.Now;
        Changed?.Invoke();
        MeetingStarted?.Invoke(info);
    }

    // Ends the meeting, clears its active state, and notifies subscribed components.
    public void EndMeeting()
    {
        if (Meeting == null)
        {
            Trace.TraceWarning("[MeetingSession] EndMeeting called but no meeting is active.");
            return;
        }

        Trace.TraceInformation($"[MeetingSession] Ending meeting {Meeting.Id}.");
        MeetingEnding?.Invoke();
        Meeting = null;
        MeetingStartTime = null;
        _activeParticipants = Array.Empty<ParticipantInfo>();
        ParticipantsChanged?.Invoke(_activeParticipants);
        MeetingEnded?.Invoke();
        Changed?.Invoke();
    }

    public void RaiseSecurityAlert(string message)
    {
        Trace.TraceWarning($"[MeetingSession] Security alert raised: {message}");
        if (uiThread != null) uiThread.Post(_ => SecurityAlertReceived?.Invoke(message), null);
        else SecurityAlertReceived?.Invoke(message);
    }
}
