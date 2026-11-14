namespace ScreenShare;

/// <summary>
/// every shared screen is sent as two streams: the full one, named after the sharer, for a
/// zoomed tile, and a small one for grid tiles, named with ThumbnailSuffix. each has its own
/// frames, acks, viewers and full-frame requests; a viewer shows either under the sharer's tile.
/// </summary>
public static class StreamLayers
{
    /// <summary>appended to a sharer's stream id to name its thumbnail stream</summary>
    public const string ThumbnailSuffix = "#thumb";

    /// <summary>the longest participant id (UTF-8 bytes) that still fits with the suffix</summary>
    public const int MaxParticipantIdBytes = ScreenShareCodec.MaxIdBytes - 6;   // ThumbnailSuffix.Length

    /// <summary>gets the id of a stream's thumbnail stream</summary>
    public static string ThumbnailOf(string streamId)
    {
        return streamId + ThumbnailSuffix;
    }

    /// <summary>true for a thumbnail stream's id</summary>
    public static bool IsThumbnail(string streamId)
    {
        return streamId.EndsWith(ThumbnailSuffix, StringComparison.Ordinal);
    }

    /// <summary>gets the sharer's stream id for either of its streams</summary>
    public static string BaseOf(string streamId)
    {
        return IsThumbnail(streamId) ? streamId[..^ThumbnailSuffix.Length] : streamId;
    }
}
