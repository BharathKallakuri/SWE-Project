// Author: Shreesh Amit Chembeti
// Roll Number: 112301046

namespace Incidents;

/// <summary>
/// Evidence related to an Incident such as photos, videos or documents.
/// Record type as it is immutable.
/// </summary>
/// <param name="Id">Id of the evidence.</param>
/// <param name="Description">Description of the evidence.</param>
/// <param name="FilePath">Path from which user uploaded evidence.</param>
/// <param name="UploadTime">Time when the evidence was uploaded.</param>
/// <param name="UploadedBy">User who uploaded the evidence.</param>
/// <param name="StoragePath">Evidence file path stored in the file synchronizer directory.</param>
public record IncidentEvidence(
    Guid Id,
    string Description,
    string? FilePath,
    DateTime UploadTime,
    string UploadedBy,
    string? StoragePath = null
);
