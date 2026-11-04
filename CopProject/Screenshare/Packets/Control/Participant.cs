using System.Net;

namespace ScreenShare;
public sealed record Participant
{
    public string Id { get; }
    public IPEndPoint Endpoint { get; }

    public Participant(string id, IPEndPoint endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));

        Id = id;
        Endpoint = endpoint;
    }

}
