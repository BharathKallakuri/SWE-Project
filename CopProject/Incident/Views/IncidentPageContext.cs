using System;
using Incident_module;

namespace Incident_module.Views;

public sealed record IncidentPageContext(
    IIncidentController IncidentController,
    Guid CurrentUserId,
    IntPtr WindowHandle
);

public sealed record IncidentDetailsPageParameters(
    IncidentPageContext Context,
    Guid IncidentId
);
