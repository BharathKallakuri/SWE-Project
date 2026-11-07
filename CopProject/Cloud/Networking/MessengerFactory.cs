namespace Cloud.Networking;

/// <summary>
/// So that there is single point of access.
/// Outer modules dont knwo my implementation they can just see my interface.
/// </summary>
public static class MessengerFactory
{
    public static IMessenger GetMessenger(string serverUrl)
    {
        return new SignalRMessenger(serverUrl);
    }

    public static IMessenger GetSilentMessenger(string serverUrl)
    {
        return new SilentDiagnosticMessenger(new SignalRMessenger(serverUrl));
    }
}
