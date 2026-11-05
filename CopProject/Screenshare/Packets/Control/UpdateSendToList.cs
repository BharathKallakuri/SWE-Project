using System.Net;

namespace ScreenShare;

/// <summary>
/// sent by the host to a sharer with the full list of viewers to send the stream to.
/// reserved for the peer-to-peer model; Networking cannot send peer-to-peer yet.
/// </summary>
public sealed record UpdateSendToList : IScreenShareMessage
{
    /// <summary>gets the id of the stream</summary>
    public string StreamId { get; }

    /// <summary>gets the viewers to send to; an empty list means send to nobody</summary>
    public IReadOnlyList<IPEndPoint> SendTo { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.UpdateSendToList;

    /// <exception cref="ArgumentException">the stream id is empty or the list contains null</exception>
    /// <exception cref="ArgumentNullException">the list is null</exception>
    public UpdateSendToList(string streamId, IReadOnlyList<IPEndPoint> sendTo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(sendTo);

        if (sendTo.Any(endpoint => endpoint == null))
        {
            throw new ArgumentException("the send-to list cannot contain null", nameof(sendTo));
        }

        StreamId = streamId;
        SendTo = Array.AsReadOnly(sendTo.ToArray());
    }
}
