// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System.Collections.ObjectModel;
using System.Diagnostics;
using Controller.Models;
using Microsoft.UI.Xaml;

namespace Controller.ViewModels;
// Manages meeting-related UI data, participant information, graphs, and security audit logs.
public sealed class MeetingViewModel : ObservableObject
{
    private readonly MeetingSession _session;
    private readonly DispatcherTimer? _timer;
    private DateTime? _meetingStartedTime;
    private readonly HashSet<string> _activeUsernames = new(StringComparer.OrdinalIgnoreCase);

    // Initializes meeting event handlers and updates the dashboard when meeting activities occur.
    public MeetingViewModel(MeetingSession session)
    {
        Trace.TraceInformation("[Controller]: Initializing MeetingViewModel.");
        _session = session;

        try
        {
            _timer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (s, e) => {
                DateTime start = _session.MeetingStartTime ?? _meetingStartedTime ?? DateTime.Now;
                TimeSpan elapsed = DateTime.Now - start;

                DurationText = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
                OnPropertyChanged(nameof(DurationText));
                ChartUpdated?.Invoke();
            };
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"[Controller]: DispatcherTimer initialization bypassed: {ex.Message}");
        }

        _session.MeetingStarted += info => {
            Trace.TraceInformation($"[Controller]: MeetingStarted event received for meeting {info.Id}.");
            _activeUsernames.Clear();
            _meetingStartedTime = _session.MeetingStartTime ?? DateTime.Now;

            string user = !string.IsNullOrWhiteSpace(_session.CurrentUserId) ? _session.CurrentUserId : "Host";
            AuditLogs.Insert(0, new SecurityAuditEvent {
                User = user,
                Activity = info.IsHost ? "Meeting created & started" : "Joined meeting",
                Status = "Success"
            });

            AuditLogs.Insert(0, new SecurityAuditEvent {
                User = user,
                Activity = "User logged in",
                Status = "Success"
            });

            _activeUsernames.Add(user);

            GraphPoints.Clear();
            GraphPoints.Add(new ParticipantDataPoint {
                Seconds = 0,
                TimeLabel = "00:00",
                Count = Math.Max(1, _session.ParticipantCount)
            });

            _timer?.Start();
            Refresh();
            ChartUpdated?.Invoke();
        };

        _session.MeetingEnded += () => {
            Trace.TraceInformation("[Controller]: MeetingEnded event received.");
            string user = !string.IsNullOrWhiteSpace(_session.CurrentUserId) ? _session.CurrentUserId : "Host";
            AuditLogs.Insert(0, new SecurityAuditEvent {
                User = user,
                Activity = "Meeting ended",
                Status = "Info"
            });

            ParticipantList.Clear();
            _timer?.Stop();
            _meetingStartedTime = null;
            _activeUsernames.Clear();
            Refresh();
            ChartUpdated?.Invoke();
        };

        _session.ParticipantCountChanged += count => {
            Trace.TraceInformation($"[Controller]: ParticipantCountChanged event received: count = {count}.");
            DateTime start = _session.MeetingStartTime ?? _meetingStartedTime ?? DateTime.Now;
            double sec = Math.Max(0, (DateTime.Now - start).TotalSeconds);
            int mins = (int)(sec / 60);
            int s = (int)(sec % 60);
            GraphPoints.Add(new ParticipantDataPoint { Seconds = sec, TimeLabel = $"{mins:D2}:{s:D2}", Count = count });
            Refresh();
            ChartUpdated?.Invoke();
        };

        _session.ParticipantsChanged += list => {
            void UpdateList()
            {
                ParticipantList.Clear();
                foreach (ParticipantInfo p in list)
                {
                    ParticipantList.Add(p);
                }
                Refresh();
            }
            var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (queue != null)
            {
                queue.TryEnqueue(UpdateList);
            }
            else
            {
                UpdateList();
            }
        };

        if (_session.InMeeting)
        {
            Trace.TraceInformation("[Controller]: Restoring state for active session.");
            _meetingStartedTime = _session.MeetingStartTime ?? DateTime.Now;
            string user = !string.IsNullOrWhiteSpace(_session.CurrentUserId) ? _session.CurrentUserId : "Host";
            _activeUsernames.Add(user);
            foreach (ParticipantInfo p in _session.ActiveParticipants)
            {
                if (!string.IsNullOrWhiteSpace(p.Username))
                {
                    _activeUsernames.Add(p.Username);
                    ParticipantList.Add(p);
                }
            }
            if (GraphPoints.Count == 0)
            {
                GraphPoints.Add(new ParticipantDataPoint { Seconds = 0, TimeLabel = "00:00", Count = Math.Max(1, _session.ParticipantCount) });
            }
            _timer?.Start();
        }
    }

    // Calculates the total elapsed meeting time for duration tracking.
    public double ElapsedSeconds
    {
        get {
            DateTime? start = _session.MeetingStartTime ?? _meetingStartedTime;
            if (start.HasValue)
            {
                return Math.Max(0, (DateTime.Now - start.Value).TotalSeconds);
            }
            return GraphPoints.Count > 0 ? GraphPoints.Max(p => p.Seconds) : 0;
        }
    }

    public string MeetingId => _session.Meeting?.Id ?? "";
    public string Port => _session.Meeting?.Port ?? "";
    public int Participants => _session.ParticipantCount;
    public string Role => _session.Meeting?.IsHost == true ? "Host" : "Participant";
    public ObservableCollection<ParticipantInfo> ParticipantList { get; } = new();

    // Maintains the currently selected participant and notifies the UI when the selection changes.
    private ParticipantInfo? _selectedParticipant;
    public ParticipantInfo? SelectedParticipant
    {
        get => _selectedParticipant;
        set {
            if (_selectedParticipant == value)
            {
                return;
            }

            _selectedParticipant = value;
            Trace.TraceInformation($"[Controller]: Selected participant changed to '{_selectedParticipant?.DisplayName ?? "None"}'.");
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedParticipant));
        }
    }
    public bool HasSelectedParticipant => SelectedParticipant != null;

    // Duration & Graph
    public string DurationText { get; private set; } = "00:00";
    public ObservableCollection<ParticipantDataPoint> GraphPoints { get; } = new();
    public ObservableCollection<SecurityAuditEvent> AuditLogs { get; } = new();
    public event Action? ChartUpdated;

    // In-meeting Security Alert Banner
    private string? _alertMessage;
    public string? AlertMessage
    {
        get => _alertMessage;
        set {
            _alertMessage = value;
            Trace.TraceWarning($"[Controller]: Alert banner updated: '{_alertMessage}'.");
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAlert));
        }
    }
    public bool HasAlert => !string.IsNullOrEmpty(_alertMessage);

    // View Flag => 0 -> normal, 1 -> Graph maximized, 2->audit maximized
    private int _maximizedView = 0;
    public int MaximizedView
    {
        get => _maximizedView;
        set {
            _maximizedView = value;
            Trace.TraceInformation($"[Controller]: MaximizedView changed to {_maximizedView}.");
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNormalVisible));
            OnPropertyChanged(nameof(IsGraphMaximized));
            OnPropertyChanged(nameof(IsAuditMaximized));
            OnPropertyChanged(nameof(IsOverlayVisible));
            ChartUpdated?.Invoke();
        }
    }

    public bool IsNormalVisible => _maximizedView == 0;
    public bool IsGraphMaximized => _maximizedView == 1;
    public bool IsAuditMaximized => _maximizedView == 2;
    public bool IsOverlayVisible => _maximizedView != 0;

    public void ShowNormal()
    {
        Trace.TraceInformation("[Controller]: ShowNormal requested.");
        MaximizedView = 0;
    }

    public void MaximizeGraph()
    {
        Trace.TraceInformation("[Controller]: MaximizeGraph requested.");
        MaximizedView = 1;
    }

    public void MaximizeAudit()
    {
        Trace.TraceInformation("[Controller]: MaximizeAudit requested.");
        MaximizedView = 2;
    }

    // Adds a security audit entry for testing or demonstrating dashboard activity.
    public void AddTestLog(string action, string status = "Info")
    {
        string user = !string.IsNullOrWhiteSpace(_session.CurrentUserId) ? _session.CurrentUserId : "User";
        Trace.TraceInformation($"[Controller]: Adding test log: action='{action}', status='{status}', user='{user}'.");
        AuditLogs.Insert(0, new SecurityAuditEvent { User = user, Activity = action, Status = status });
    }
}
