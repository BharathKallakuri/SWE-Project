using Incident_module;
using Incident_module.Implementations;
using Filesync;
using Persistence;
using WinRT.IncidentVtableClasses;

namespace Incidents.Test;

/// <summary>
/// Test class for update requests to IncidentController
/// </summary>
[TestClass]
public sealed class IncidentControllerUpdatesTest
{
    private IncidentController _incidentController = null!;
    private readonly List<Incident> _mockIncidents = [];

    /// <summary>
    /// Initialiazes IncidentController
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        await SetupIncidentController();
        await SetupMockIncidents();
    }

    private async Task SetupIncidentController()
    {
        IStorage cloudStorageService = new IncidentCloudStorageService();
        IFileOperations fileOperationsService = new IncidentFileSynchroniser();
        _incidentController = await IncidentController.CreateAsync(cloudStorageService, fileOperationsService);
    }

    private readonly List<CreateIncidentRequest> _createIncidentRequests = [
        new CreateIncidentRequest(
            title: "Test Incident 1",
            description: "This is a test incident 1.",
            createdBy: Guid.Empty,
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: []
        ),
        new CreateIncidentRequest(
            title: "Test Incident 2",
            description: "This is a test incident 2.",
            createdBy: Guid.Empty,
            category: IncidentCategory.DrunkDriving,
            severity: IncidentSeverity.High,
            location: new IncidentLocation(Latitude: 1.0, Longitude: 1.0),
            evidences: []
        ),
        new CreateIncidentRequest(
            title: "Test Incident 3",
            description: "This is a test incident 3.",
            createdBy: Guid.Empty,
            category: IncidentCategory.Theft,
            severity: IncidentSeverity.Low,
            location: new IncidentLocation(Latitude: 3.0, Longitude: 3.0),
            evidences: []
        )
    ];

    /// <summary>
    /// Creates few incidents for testing
    /// </summary>
    /// <returns></returns>
    private async Task SetupMockIncidents()
    {
        await Task.WhenAll(_createIncidentRequests.Select(async (request) => {
            Incident incident = await _incidentController.CreateIncident(request);
            _mockIncidents.Add(incident);
        }));
    }


    [TestMethod]
    [DataRow(null, IncidentStatus.InProgress)]
    public async Task UpdateCategory_InvalidIncident_ThrowsArgumentNullException(Incident incident, IncidentStatus status)
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateStatus(incident, status);
        });
    }

    [TestMethod]
    [DataRow(null, IncidentStatus.InProgress)]
    public async Task UpdateLocation_InvalidIncident_ThrowsArgumentNullException(Incident incident, IncidentStatus status)
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateStatus(incident, status);
        });
    }

    [TestMethod]
    [DataRow(null, IncidentStatus.InProgress)]
    public async Task UpdateSeverity_InvalidIncident_ThrowsArgumentNullException(Incident incident, IncidentStatus status)
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateStatus(incident, status);
        });
    }


    [TestMethod]
    [DataRow(null, IncidentStatus.InProgress)]
    public async Task UpdateStatus_InvalidIncident_ThrowsArgumentNullException(Incident incident, IncidentStatus status)
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => {
            await _incidentController.UpdateStatus(incident, status);
        });
    }
}
