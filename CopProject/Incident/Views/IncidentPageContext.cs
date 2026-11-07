using System;
using System.Diagnostics.CodeAnalysis;
using Incident_module;

namespace Incident_module.Views;

[ExcludeFromCodeCoverage]
public sealed record IncidentPageContext(
    IIncidentController IncidentController,
    Guid CurrentUserId,
    IntPtr WindowHandle
);

[ExcludeFromCodeCoverage]
public sealed record IncidentDetailsPageParameters(
    IncidentPageContext Context,
    Guid IncidentId
);

[ExcludeFromCodeCoverage]
public sealed record IncidentCreatePageWithLocation(
    IncidentPageContext Context,
    double Latitude,
    double Longitude,
    string? Address
);
