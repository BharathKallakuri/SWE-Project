// Author : Saideep
// Roll No : 112301018
using System;
using System.Linq ;
using System.Threading;
using Controller;
using Controller.Models;
using Utils;
using Xunit ;

namespace Controller.Test;

public class ControllerMeetingServiceTests
{
    private class DummyNotificationHandler:IUserNotificationHandler
    {
        public void OnUserJoined(ControllerUser user)
        {
        }
        public void OnUserLeft(ControllerUser user)
        {
        }
    }

    [Fact]
    public void NotificationHandlersRegistration()
    {
        var service = new ControllerMeetingService();
        var handler = new DummyNotificationHandler();
        service.RegisterNotificationHandler(handler);
        service.RegisterNotificationHandler(handler);
        service.RegisterNotificationHandler(null!);
        service.UnregisterNotificationHandler(handler);
        service.UnregisterNotificationHandler(null!);
    }

    [Fact]
    public void CreateMeetingHost()
    {
        var session = new MeetingSession{
            CurrentUserId = "HostUser",
            CurrentUserEmail = "host@test.com",
            CurrentUserProfilePicture = new byte[] {1,2,3}
        };
        var service = new ControllerMeetingService();
        service.Initialize(session);
        var handler = new DummyNotificationHandler();
        service.RegisterNotificationHandler(handler);

        var (id, port) = service.CreateMeeting();
        Assert.NotNull(service.ActiveController);
        Assert.True(service.ActiveController is HostController);
        Assert.False(string.IsNullOrEmpty(id));
        Assert.False(string.IsNullOrEmpty(port));

        session.StartMeeting(new MeetingInfo(id,port,IsHost:true));
        Assert.True(session.InMeeting);
        Assert.Equal(1,session.ParticipantCount);
        Assert.NotEmpty(session.ActiveParticipants);

        byte[] newPicture = new byte[] { 4, 5, 6 };
        service.UpdateProfilePicture(newPicture);
        Assert.Equal(newPicture, service.ActiveController.CurrentUser?.ProfilePicture);
        Assert.Equal(newPicture, session.ActiveParticipants.First().ProfilePicture);

        session.EndMeeting();
        Assert.False(session.InMeeting);
        Assert.Null(service.ActiveController);
    }

    [Fact]
    public void CreateMeeting_Default()
    {
        var session = new MeetingSession();
        var service = new ControllerMeetingService();
        service.Initialize(session);
        var (id, port) = service.CreateMeeting();
        Assert.Equal("Host", service.ActiveController?.CurrentUser?.Username);
        Assert.Equal("Host@smail.iitpkd.ac.in", service.ActiveController?.CurrentUser?.Gmail);
        session.EndMeeting();
    }

    
}