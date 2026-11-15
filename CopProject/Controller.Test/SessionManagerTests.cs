// Author : Saideep
// Roll No : 112301018.
using Controller;
using Utils;
using Xunit;

namespace Controller.Test;

public class SessionManagerTests
{
    [Fact]
    public void CreateAndStartSession()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var nonHost = new ControllerUser("nonHost","nonhost@test.com",null,"10.10.10.11",9991, isHost: false);
        var session = manager.CreateSession(host);
        bool nonHostStart = manager.StartSession(nonHost);
        Assert.False(nonHostStart);
        bool eventFired = false ;
        manager.UserJoined += (s,e) => {eventFired = true;};
        bool started = manager.StartSession(host);
        Assert.True(started);
        Assert.True(eventFired);
        bool secondStart = manager.StartSession(host);
        Assert.False(secondStart);
    }


    [Fact]
    public void JoinAndLeaveSession()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990,isHost: true);
        manager.CreateSession(host);
        manager.StartSession(host);
        bool duplicateDetected = false ;
        manager.DuplicateUserDetected += (s,e) => {duplicateDetected = true;};
        bool userLeftFired = false ;
        manager.UserLeft += (s,e) => {userLeftFired = true;};
        var client = new ControllerUser("ClientUser", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        bool joined = manager.JoinSession(client);
        Assert.True(joined);
        Assert.Equal(2, manager.GetActiveUserCount());
        Assert.Equal(2, manager.GetActiveUsers().Count);
        bool joinedAgain = manager.JoinSession(client);
        Assert.False(joinedAgain);
        Assert.True(duplicateDetected);
        bool leftUnknown = manager.LeaveSession("unknownKey");
        Assert.False(leftUnknown);
        bool left = manager.LeaveSession(client.UserKey);
        Assert.True(left);
        Assert.True(userLeftFired);
        Assert.Equal(1, manager.GetActiveUserCount());
    }

    [Fact]
    public void EndSession()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("HostUser", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        var session = manager.CreateSession(host);
        manager.StartSession(host);
        bool nonHostEnd = manager.EndSession("non-host-key");
        Assert.False(nonHostEnd);
        bool meetingEndedFired = false ;
        manager.MeetingEnded += (s,e) => {meetingEndedFired = true;};
        bool ended = manager.EndSession(host.UserKey);
        Assert.True(ended);
        Assert.True(meetingEndedFired);
        Assert.False(manager.HasActiveSession());
        var session2 = manager.CreateSession(host);
        session2!.End();
        bool endedAgain = manager.EndSession(host.UserKey);
        Assert.False(endedAgain);
    }

    [Fact]
    public void OperationsWithoutActiveSession()
    {
        var manager = new SessionManager();
        var dummy = new ControllerUser("Dummy", "dummy@test.com", null, "10.10.10.10", 9990, isHost: false);
        Assert.False(manager.StartSession(dummy));
        Assert.False(manager.JoinSession(dummy));
        Assert.False(manager.LeaveSession("unknownKey"));
        Assert.False(manager.EndSession("unknownKey"));
        Assert.Equal(0, manager.GetActiveUserCount());
        Assert.Empty(manager.GetActiveUsers());
    }

    [Fact]
    public void SessionMessageListener_HandleJoinAndLeave()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("Host", "host@test.com", null, "10.10.10.10", 9990, isHost: true);
        manager.CreateSession(host);
        manager.StartSession(host);
        var listener = new SessionMessageListener(manager);
        var client = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        string joinMsg = Protocol.FormatJoinRequest(client);
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(joinMsg));
        Assert.Equal(2, manager.GetActiveUserCount());
        string leaveMsg = Protocol.FormatLeaveRequest(client.UserKey);
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(leaveMsg));
        Assert.Equal(1, manager.GetActiveUserCount());
    }

    [Fact]
    public void SessionMessageListener_MalformedAndEmptyMessages()
    {
        var manager = new SessionManager();
        var listener = new SessionMessageListener(manager);
        Assert.Throws<ArgumentNullException>(() => new SessionMessageListener(null!));
        var client = new ControllerUser("Client", "client@test.com", null, "10.10.10.10", 9991, isHost: false);
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(Protocol.FormatJoinRequest(client)));
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes(Protocol.FormatLeaveRequest(client.UserKey)));
        listener.OnMessageReceived(Array.Empty<byte>());
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes("NO_SEPARATOR"));
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes("UNKNOWN_TYPE|payload"));
        listener.OnMessageReceived(System.Text.Encoding.UTF8.GetBytes("JOIN|not-valid-json"));
    }

    [Fact]
    public void GetSessionAndClearSession()
    {
        var manager = new SessionManager();
        var host = new ControllerUser("Host","host@test.com",null,"10.10.10.10",9990,isHost:true);
        Assert.Null(manager.GetSession());
        manager.CreateSession(host);
        Assert.NotNull(manager.GetSession());
        manager.ClearSession();
        Assert.Null(manager.GetSession());
        Assert.False(manager.HasActiveSession());
    }

    [Fact]
    public void SessionInfoEdgeCases(){
        Assert.Throws<ArgumentNullException>(() => new SessionInfo(null!));
        var host = new ControllerUser("Host","host@test.com",null,"10.10.10.10",9990,isHost:true);
        var session = new SessionInfo(host);
        var client1 = new ControllerUser("Client1","client1@test.com",null,"10.10.10.10",9991,isHost:false);
        Assert.True(session.AddUser(client1));
        Assert.False(session.AddUser(client1));
        Assert.True(session.End());
        var client2 = new ControllerUser("Client2","client2@test.com",null,"10.10.10.10",9992,isHost:false);
        Assert.False(session.AddUser(client2));
        Assert.False(session.End());
    }
}