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
}
