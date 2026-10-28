using System;

namespace Incident_module.ViewModels.Utils;

public static class AppServices
{
    public static IIncidentController IncidentController { get; set; } = null!;
    public static INavigationService NavigationService { get; set; } = null!;
    public static Guid CurrentUserId { get; set; }
}
