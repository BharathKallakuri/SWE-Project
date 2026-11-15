// Author = Simeon Kandan Sonar
// Roll Number = 112301031

namespace Cloud.Networking.Server.Models;

/// <summary>
/// A persistent connection to a server
/// </summary>
/// <param name="ConnectionId">the id of a <see cref="LiveConnection"/></param>
/// <param name="ConnectedAt">the creation timestamp of the <see cref="LiveConnection"/></param>
public sealed record LiveConnection(
    string ConnectionId,
    DateTimeOffset ConnectedAt
);
