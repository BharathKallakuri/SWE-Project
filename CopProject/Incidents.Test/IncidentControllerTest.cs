// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security.Policy;
using Filesync;
using Incident_module;
using Incident_module.Implementations;
using Persistence;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;


namespace Incidents.Test;

[TestClass]
public sealed class IncidentControllerTest
{
    private IncidentController _incidentController = null!;

    private static readonly object?[] s_filters =
    [
        // No filter
        null,

        // Individual filters
        new IncidentFilter(Title: "Test Incident 1"),
        new IncidentFilter(Status: IncidentStatus.InProgress),
        new IncidentFilter(Category: IncidentCategory.Accident),
        new IncidentFilter(Severity: IncidentSeverity.High),
        new IncidentFilter(FromDateTime: new DateTime(2026, 1, 1)),
        new IncidentFilter(ToDateTime: new DateTime(2026, 12, 31)),
        new IncidentFilter(CreatedBy: Guid.NewGuid()),

        // Multiple filters
        new IncidentFilter(
            Category: IncidentCategory.Accident,
            Severity: IncidentSeverity.High,
            Status: IncidentStatus.Reported
        ),

        // Date range
        new IncidentFilter(
            FromDateTime: new DateTime(2026, 1, 1),
            ToDateTime: new DateTime(2026, 12, 31)
        ),
        
        // All filters
        new IncidentFilter(
            Title: "Test Incident",
            Status: IncidentStatus.InProgress,
            Category: IncidentCategory.Accident,
            Severity: IncidentSeverity.High,
            FromDateTime: new DateTime(2026, 1, 1, 0, 0, 0),
            ToDateTime: new DateTime(2026, 12, 31, 23, 59, 59),
            CreatedBy: Guid.NewGuid(),
            Location: new IncidentLocationFilter(
                Latitude: 10.80,
                Longitude: 76.72,
                Address: "IIT Palakkad, Kerala, India",
                Radius: 10.0
            )
        )
    ];

    public static IEnumerable<object?[]> IncidentFilters =>
        s_filters.Select(filter => new object?[] { filter });

    /// <summary>
    /// Initialiazes IncidentController
    /// </summary>
    [TestInitialize]
    public async Task InitiliazeIncidentController()
    {
        IStorage cloudStorageService = new IncidentCloudStorageService();
        IFileOperations fileOperationsService = new IncidentFileSynchroniser();
        _incidentController = await IncidentController.CreateAsync(cloudStorageService, fileOperationsService);
    }

    /// <summary>
    /// Checks that creating an incident with a null argument throws an ArgumentNullException.
    /// </summary>
    [TestMethod]
    public async Task CreateNullIncident()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
          () => _incidentController.CreateIncident(null!));
    }

    [TestMethod]
    public async Task CreateIncident()
    {
        var incident = new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: Guid.NewGuid(),
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<IncidentEvidence>()
        );

        Incident createdIncident = await _incidentController.CreateIncident(incident);

        Assert.IsNotNull(createdIncident);
    }

    /// <summary>
    /// Get incident by id and check that the retrieved incident matches the created incident.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task GetIncidentById()
    {
        var incident = new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: Guid.NewGuid(),
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<IncidentEvidence>()
        );

        Incident createdIncident = await _incidentController.CreateIncident(incident);

        Incident retrievedIncident = await _incidentController.GetIncidentById(createdIncident.Id);

        Assert.IsNotNull(createdIncident);
        Assert.IsNotNull(retrievedIncident);
        Assert.AreEqual(createdIncident.Id, retrievedIncident.Id);
        Assert.AreEqual(incident.Title, retrievedIncident.Title);
        Assert.AreEqual(incident.Description, retrievedIncident.Description);
        Assert.AreEqual(incident.CreatedBy, retrievedIncident.CreatedBy);
        Assert.AreEqual(incident.Status, retrievedIncident.Status);
        Assert.AreEqual(incident.Category, retrievedIncident.Category);
        Assert.AreEqual(incident.Severity, retrievedIncident.Severity);
        Assert.AreEqual(incident.Location.Latitude, retrievedIncident.Location.Latitude);
        Assert.AreEqual(incident.Location.Longitude, retrievedIncident.Location.Longitude);
    }

    /// <summary>
    /// Creates few incidents for testing
    /// </summary>
    /// <returns></returns>
    private async Task CreateMockIncidents()
    {
        var incident1 = new CreateIncidentRequest(
            title: "Test Incident 1",
            description: "This is a test incident 1.",
            createdBy: Guid.NewGuid(),
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<IncidentEvidence>()
        );
        var incident2 = new CreateIncidentRequest(
            title: "Test Incident 2",
            description: "This is a test incident 2.",
            createdBy: Guid.NewGuid(),
            category: IncidentCategory.DrunkDriving,
            severity: IncidentSeverity.High,
            location: new IncidentLocation(Latitude: 1.0, Longitude: 1.0),
            evidences: new List<IncidentEvidence>()
        );
        var incident3 = new CreateIncidentRequest(
            title: "Test Incident 3",
            description: "This is a test incident 3.",
            createdBy: Guid.NewGuid(),
            category: IncidentCategory.Theft,
            severity: IncidentSeverity.Low,
            location: new IncidentLocation(Latitude: 2.0, Longitude: 2.0),
            evidences: new List<IncidentEvidence>()
        );

        await _incidentController.CreateIncident(incident1);
        await _incidentController.CreateIncident(incident2);
        await _incidentController.CreateIncident(incident3);
    }

    /// <summary>
    /// Get incident by id and check that the retrieved incident matches the created incident.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    [DynamicData(nameof(IncidentFilters))]
    public async Task GetAllIncidents(IncidentFilter filter)
    {
        await CreateMockIncidents();
        List<Incident> incidents = await _incidentController.GetAllIncidents(filter);

        Assert.IsNotNull(incidents);
    }
}
