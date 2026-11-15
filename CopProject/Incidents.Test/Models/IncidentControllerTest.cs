// Author: Shreesh Amit Chembeti
// Roll Number: 112301046

using FileSynchroniser.Models;
using Incidents.Test.Implementations;
using Cloud.Persistence;


namespace Incidents.Test.Models;

/// <summary>
/// Test Class for Retrieving Incidents, Adding Evidence, Refresh.
/// </summary>
[TestClass]
public sealed class IncidentControllerTest
{
    private readonly string _currentUserId = "test_user_id";
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
        new IncidentFilter(CreatedBy: "person1"),

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

        // Location Filter
        new IncidentFilter(
            Location: new IncidentLocationFilter(
                Latitude: 10.80,
                Longitude: 76.72,
                Address: "IIT Palakkad, Kerala, India",
                Radius: 10.0
            )
        ),
        
        // All filters
        new IncidentFilter(
            Title: "Test Incident",
            Status: IncidentStatus.InProgress,
            Category: IncidentCategory.Accident,
            Severity: IncidentSeverity.High,
            FromDateTime: new DateTime(2026, 1, 1, 0, 0, 0),
            ToDateTime: new DateTime(2026, 12, 31, 23, 59, 59),
            CreatedBy: "person1",
            Location: new IncidentLocationFilter(
                Latitude: 10.80,
                Longitude: 76.72,
                Address: "IIT Palakkad, Kerala, India",
                Radius: 10.0
            )
        )
    ];
    private List<Incident> _mockIncidents = new List<Incident>();

    public static IEnumerable<object?[]> IncidentFilters =>
        s_filters.Select(filter => new object?[] { filter });

    /// <summary>
    /// Initialiazes IncidentController
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        await SetupIncidentController();
        SetupMockIncidents();
    }

    /// <summary>
    /// Initializes Mock Incident Controller
    /// </summary>
    private async Task SetupIncidentController()
    {
        IStorage cloudStorageService = new IncidentCloudStorageService();
        IFileOperations fileOperationsService = new IncidentFileSynchroniser();
        _incidentController = await IncidentController.CreateAsync(_currentUserId, cloudStorageService, fileOperationsService);
    }

    /// <summary>
    /// Creating an incident with a null argument throws an ArgumentNullException.
    /// </summary>
    [TestMethod]
    public async Task CreateIncident_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
          () => _incidentController.CreateIncident(null!));
    }

    /// <summary>
    /// Creating a valid incident request returns a valid incident.
    /// </summary>
    [TestMethod]
    public async Task CreateIncident_ValidArgument_ReturnsCreatedIncident()
    {
        var incident = new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<(IncidentEvidence Evidence, byte[] Content)>()
        );

        Incident createdIncident = await _incidentController.CreateIncident(incident);

        Assert.IsNotNull(createdIncident);
    }

    /// <summary>
    /// Get incident by id and check that the retrieved incident matches the created incident.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task GetIncident_ValidId_ReturnsRetrievedIncident()
    {
        var incident = new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<(IncidentEvidence Evidence, byte[] Content)>()
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
    private void SetupMockIncidents()
    {
        _mockIncidents.AddRange(
            [
            new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 1",
            description: "This is a test incident 1.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            status: IncidentStatus.Reported,
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidence: [],
            changes: [],
            edges: []
            ),
       new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 2",
            description: "This is a test incident 2.",
            createdBy: "person2",
            category: IncidentCategory.DrunkDriving,
            severity: IncidentSeverity.High,
            status: IncidentStatus.InProgress,
            location: new IncidentLocation(Latitude: 1.0, Longitude: 1.0),
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            evidence: [],
            changes: [],
            edges: []
         ),
       new Incident(
            id: Guid.NewGuid(),
            title: "Test Incident 3",
            description: "This is a test incident 3.",
            createdBy: "person1",
            category: IncidentCategory.Theft,
            severity: IncidentSeverity.Low,
            status: IncidentStatus.InProgress,
            location: new IncidentLocation(
                Latitude: 10.80,
                Longitude: 76.72,
                Address: "IIT Palakkad, Kerala, India"
            ),
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            evidence: [],
            changes: [],
            edges: []
         )
        ]);
    }

    /// <summary>
    /// Refetches and updates the local cache of incidents
    /// </summary>
    [TestMethod]
    public async Task RefreshIncidents_ReturnsTask()
    {
        await _incidentController.Refresh();
    }

    /// <summary>
    /// Gets all incidents with a valid filter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IncidentFilters))]
    public async Task GetAllIncidents_ValidFilter_ReturnsIncidents(IncidentFilter filter)
    {
        List<Incident> incidents = await _incidentController.GetAllIncidents(filter);

        Assert.IsNotNull(incidents);
    }

    /// <summary>
    /// Add evidence to a null incident throws ArgumentNullException.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task AddEvidence_NullIncident_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _incidentController.AddEvidence(null!, []));
    }

    /// <summary>
    /// Add null evidence to a valid incident throws ArgumentNullException.
    /// </summary>
    [TestMethod]
    public async Task AddEvidence_NullEvidence_ThrowsArgumentNullException()
    {
        if (_mockIncidents.Count == 0)
        {
            Assert.Fail("No incidents created for testing.");
        }

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => _incidentController.AddEvidence(_mockIncidents[0], null!));
    }

    /// <summary>
    /// Add Evidence to a resolved Incident throws InvalidOperationException
    /// </summary>
    [TestMethod]
    public async Task AddEvidence_ResolvedIncident_ThrowsInvalidOperationException()
    {
        Incident resolvedIncident = new Incident(
            id: Guid.NewGuid(),
            title: "Resolved Incident",
            description: "This incident is resolved.",
            createdBy: "person1",
            status: IncidentStatus.Resolved,
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            creationTime: DateTime.UtcNow,
            updateTime: DateTime.UtcNow,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidence: [],
            changes: [],
            edges: []
        );

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _incidentController.AddEvidence(resolvedIncident, []));
    }

    /// <summary>
    /// Adding evidence when ETag of Incident not available in cloud storage thows InvalidOperationException
    /// </summary>
    [TestMethod]
    public async Task AddEvidence_ETagNotFound_ThrowsInvalidOperationException()
    {
        Incident? incident = _mockIncidents.Find(i => i.Status != IncidentStatus.Resolved);

        if (incident == null)
        {
            Assert.Fail("No incidents created for testing.");
        }

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _incidentController.AddEvidence(incident, new List<(IncidentEvidence, byte[])> {
                (new IncidentEvidence(
                    Id: Guid.NewGuid(),
                    Description: "Test Evidence",
                    FilePath: "/path/to/test_image.jpg",
                    UploadTime: DateTime.UtcNow,
                    UploadedBy: "person1"
                    ), []
                )
            }));
    }

    /// <summary>
    /// Add evidence to a valid Incident returns true
    /// </summary>
    [TestMethod]
    public async Task AddEvidence_ValidIncident_ReturnsTrue()
    {
        Incident? incident = await _incidentController.CreateIncident(new CreateIncidentRequest(
            title: "Test Incident",
            description: "This is a test incident.",
            createdBy: "person1",
            category: IncidentCategory.Accident,
            severity: IncidentSeverity.Medium,
            location: new IncidentLocation(Latitude: 0.0, Longitude: 0.0),
            evidences: new List<(IncidentEvidence Evidence, byte[] Content)>()
        ));

        if (incident == null)
        {
            Assert.Fail("No incidents created for testing.");
        }

        Assert.IsTrue(await _incidentController.AddEvidence(incident, new List<(IncidentEvidence, byte[])> {
            (new IncidentEvidence(
                Id: Guid.NewGuid(),
                Description: "Test Evidence",
                FilePath: "/path/to/test_image.jpg",
                UploadTime: DateTime.UtcNow,
                UploadedBy: "person1"
            ), [])
        }));
    }
}
