// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Controller.Models;
using Controller.ViewModels;
using Xunit;

namespace Controller.Test;

public class MeetingViewModelTests
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
    public void Constructor_SetsDefaultValues()
    {
        var vm = new MeetingViewModel(CreateSessionWithoutSyncContext());

        Assert.Equal("", vm.MeetingId);
        Assert.Equal("", vm.Port);
        Assert.Equal(1, vm.Participants);
        Assert.Equal("Participant", vm.Role);
        Assert.Equal("00:00", vm.DurationText);
        Assert.True(vm.IsNormalVisible);
        Assert.Empty(vm.GraphPoints);
        Assert.Empty(vm.ParticipantList);
    }

    [Theory]
    [InlineData(true, "Host")]
    [InlineData(false, "Participant")]
    public void Role_ReturnsCorrectValue(bool isHost, string expected)
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        session.StartMeeting(new MeetingInfo("m1", "8080", isHost));

        var vm = new MeetingViewModel(session);

        Assert.Equal(expected, vm.Role);
    }

    [Fact]
    public void SelectedParticipant_AndAlert_WorkCorrectly()
    {
        var vm = new MeetingViewModel(CreateSessionWithoutSyncContext());
        var participant = new ParticipantInfo { Username = "Sai" };

        vm.SelectedParticipant = participant;
        Assert.Equal(participant, vm.SelectedParticipant);
        Assert.True(vm.HasSelectedParticipant);

        vm.SelectedParticipant = null;
        Assert.False(vm.HasSelectedParticipant);

        vm.AlertMessage = "Poor connection";
        Assert.True(vm.HasAlert);

        vm.AlertMessage = null;
        Assert.False(vm.HasAlert);
    }

    [Fact]
    public void MaximizedView_ChangesCorrectly()
    {
        var vm = new MeetingViewModel(CreateSessionWithoutSyncContext());

        vm.MaximizeGraph();
        Assert.True(vm.IsGraphMaximized);

        vm.MaximizeAudit();
        Assert.True(vm.IsAuditMaximized);

        vm.ShowNormal();
        Assert.True(vm.IsNormalVisible);
    }

    [Fact]
    public void AddTestLog_AddsAuditEntry()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        session.CurrentUserId = "bala";
        var vm = new MeetingViewModel(session);

        vm.AddTestLog("File uploaded", "Success");

        Assert.Single(vm.AuditLogs);
        Assert.Equal("bala", vm.AuditLogs[0].User);
        Assert.Equal("File uploaded", vm.AuditLogs[0].Activity);
    }

    [Fact]
    public void MeetingStartedAndEnded_UpdateAuditLogs()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        var vm = new MeetingViewModel(session);

        session.StartMeeting(new MeetingInfo("m1", "8080", true));

        Assert.Equal(2, vm.AuditLogs.Count);
        Assert.Single(vm.GraphPoints);

        session.EndMeeting();

        Assert.Contains(vm.AuditLogs, x => x.Activity == "Meeting ended");
    }

    [Fact]
    public void ParticipantCountChanged_UpdatesGraph()
    {
        SynchronizationContext? prev = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var session = new MeetingSession();
            session.StartMeeting(new MeetingInfo("m1", "8080", true));

            var vm = new MeetingViewModel(session);
            session.ParticipantCount = 5;

            Assert.Contains(vm.GraphPoints, x => x.Count == 5);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }

    [Fact]
    public void ParticipantsChanged_DetectsJoinAndLeave()
    {
        SynchronizationContext? prev = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var session = new MeetingSession();
            session.StartMeeting(new MeetingInfo("m1", "8080", true));

            var vm = new MeetingViewModel(session);

            session.ActiveParticipants = new List<ParticipantInfo>
            {
                new() { Username = "bala" },
                new() { Username = "kalyan" }
            };

            Assert.Equal(2, vm.ParticipantList.Count);

            vm.SelectedParticipant = vm.ParticipantList.First(x => x.Username == "kalyan");

            session.ActiveParticipants = new List<ParticipantInfo>
            {
                new() { Username = "bala" },
                new() { Username = "sai" }
            };

            Assert.Contains(vm.AuditLogs, x => x.Activity == "Joined meeting" && x.User == "sai");
            Assert.Contains(vm.AuditLogs, x => x.Activity == "Left meeting" && x.User == "kalyan");
            Assert.Null(vm.SelectedParticipant);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }

    [Fact]
    public void ElapsedSeconds_ReturnsCorrectValue()
    {
        MeetingSession session = CreateSessionWithoutSyncContext();
        var vm = new MeetingViewModel(session);

        Assert.Equal(0, vm.ElapsedSeconds);

        vm.GraphPoints.Add(new ParticipantDataPoint { Seconds = 45 });

        Assert.Equal(45, vm.ElapsedSeconds);
    }
}
