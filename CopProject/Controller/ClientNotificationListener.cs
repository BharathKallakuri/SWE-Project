//K.Bala Krishna
//112301015

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Networking;
using Utils;

namespace Controller;

// Listens for broadcast notifications from the host and raises events.
internal class ClientNotificationListener : IMessageListener
{
    private readonly ClientController _client;

    public ClientNotificationListener(ClientController client)
    {
        _client = client;
    }

    public void OnMessageReceived(byte[] message)
    {
        if (message == null || message.Length == 0)
        {
            Trace.TraceWarning("[Controller]: Client received empty notification.");
            return;
        }

        string messageString = Encoding.UTF8.GetString(message);
        ParsedMessage? parsed = Protocol.Parse(messageString);
        if (parsed == null)
        {
            Trace.TraceWarning($"[Controller]: Client received malformed notification: {messageString}");
            return;
        }
        
        switch (parsed.Type)
        {
            case Protocol.NotifyUserJoined:
            {
                try
                {
                    // Payload: TargetUserJson | AllUsersListJson.
                    ControllerUser? joinedUser = JsonSerializer.Deserialize<ControllerUser>(parsed.Payload);
                    if (joinedUser != null)
                    {
                        _client.IsJoined = true;
                        _client.JoinResponseEvent.Set();
                        if (parsed.Extra.Length > 0 && !string.IsNullOrWhiteSpace(parsed.Extra[0]))
                        {
                            var allUsers = JsonSerializer.Deserialize<List<ControllerUser>>(parsed.Extra[0]);
                            if (allUsers != null)
                                _client.SyncUserList(allUsers);
                        }
                        _client.UpdateUserCount(_client.GetActiveUserCount());
                        _client.RaiseUserJoined(new UserJoinedEventArgs(joinedUser, _client.GetActiveUserCount()));
                        if (!_client.InitialSync)
                        {
                            _client.InitialSync = true;
                            //First call always notifies about the Current User.
                            if (_client.CurrentUser != null)
                            {
                                _client.NotifyHandlersUserJoined(_client.CurrentUser);
                            }
                            foreach (var user in _client.GetActiveUsers())
                            {
                                if(user.UserKey != _client.CurrentUser?.UserKey)
                                {
                                    _client.NotifyHandlersUserJoined(user);
                                }
                            }
                        }
                        else
                        {
                            _client.NotifyHandlersUserJoined(joinedUser);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"[Controller]: Failed to parse NotifyUserJoined: {ex.Message}");
                }
                break;
            }

            case Protocol.NotifyUserLeft:
            {
                try
                {
                    // Payload: TargetUserJson | AllUsersListJson.
                    ControllerUser? leftUser = JsonSerializer.Deserialize<ControllerUser>(parsed.Payload);
                    if (leftUser != null)
                    {
                        if (parsed.Extra.Length > 0 && !string.IsNullOrWhiteSpace(parsed.Extra[0]))
                        {
                            var allUsers = JsonSerializer.Deserialize<List<ControllerUser>>(parsed.Extra[0]);
                            if (allUsers != null)
                                _client.SyncUserList(allUsers);
                        }
                        _client.UpdateUserCount(_client.GetActiveUserCount());
                        _client.RaiseUserLeft(new UserLeftEventArgs(leftUser, _client.GetActiveUserCount()));
                        _client.NotifyHandlersUserLeft(leftUser);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"[Controller]: Failed to parse NotifyUserLeft: {ex.Message}");
                }
                break;
            }

            case Protocol.NotifySessionEnded:
            {
                try
                {
                    ControllerUser? hostUser = JsonSerializer.Deserialize<ControllerUser>(parsed.Payload);
                    if (hostUser != null)
                    {
                        _client.RaiseMeetingEnded(new MeetingEndedEventArgs(hostUser));
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"[Controller]: Failed to parse NotifySessionEnded: {ex.Message}");
                }
                break;
            }

            case Protocol.NotifySecurityAlert:
            {
                string[] parts = parsed.Payload.Split('|');
                string username = parts.Length > 0 ? parts[0] : "User";
                string gmail = parts.Length > 1 ? parts[1] : "";
                string ip = parts.Length > 2 ? parts[2] : "";
                _client.RaiseSecurityAlert($"Duplicate join blocked: '{gmail}' attempted to join from {ip} on another device.");
                break;
            }
            
            case Protocol.NotifyJoinRejected:
            {
                if (!_client.IsJoined)
                {
                    string reason = !string.IsNullOrWhiteSpace(parsed.Payload) 
                        ? parsed.Payload 
                        : "User with this email is already in the meeting.";
                    _client.LastErrorMessage = reason;
                    _client.JoinResponseEvent.Set();
                }
                break;
            }
            
            default:
                Trace.TraceWarning($"[Controller]: Client received unknown notification type: {parsed.Type}");
                break;
        }
    }
}
