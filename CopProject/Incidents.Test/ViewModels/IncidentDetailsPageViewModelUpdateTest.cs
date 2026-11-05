using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Incident_module;
using Incident_module.Implementations;
using Incident_module.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

[TestClass]
public class IncidentDetailsPageViewModelUpdateTest
{
    private IIncidentController _controller = null!;
    private IncidentDetailsPageViewModel _viewModel = null!;

    private readonly Guid _currentUserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Initializes the real IncidentController and IncidentDetailsPageViewModel
    /// before each test.
    /// </summary>
    [TestInitialize]
    public async Task Setup()
    {
        IncidentCloudStorageService storageService = new();
        IncidentFileSynchroniser fileSynchroniser = new();

        _controller = await IncidentController.CreateAsync(storageService, fileSynchroniser);

        _viewModel = new IncidentDetailsPageViewModel(_controller, _currentUserId);
    }

    // SaveChangesAsync tests

    /// <summary>
    /// Test 1: SaveChangesAsync does nothing when no incident is loaded.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_NoIncident_DoesNothing()
    {
        _viewModel.IsBusy = false;
        _viewModel.ErrorMessage = "Existing error.";

        await _viewModel.SaveChangesAsync();

        Assert.IsNull(_viewModel.Incident);
        Assert.IsFalse(_viewModel.IsBusy);
        Assert.AreEqual("Existing error.", _viewModel.ErrorMessage);
    }

    /// <summary>
    /// Test 2: Saving changed status, category, severity, and location
    /// updates the incident through the real controller.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_AllFieldsChanged_UpdatesIncident()
    {
        await LoadCreatedIncidentAsync();

        _viewModel.EditableStatus = IncidentStatus.InProgress;
        _viewModel.EditableCategory = IncidentCategory.Theft;
        _viewModel.EditableSeverity = IncidentSeverity.Critical;
        _viewModel.EditableLatitude = 11.0;
        _viewModel.EditableLongitude = 77.0;
        _viewModel.EditableAddress = "Updated location";

        await _viewModel.SaveChangesAsync();

        Assert.IsNotNull(_viewModel.Incident);
        Assert.AreEqual(IncidentStatus.InProgress, _viewModel.Incident.Status);
        Assert.AreEqual(IncidentCategory.Theft, _viewModel.Incident.Category);
        Assert.AreEqual(IncidentSeverity.Critical, _viewModel.Incident.Severity);
        Assert.AreEqual(11.0, _viewModel.Incident.Location.Latitude);
        Assert.AreEqual(77.0, _viewModel.Incident.Location.Longitude);
        Assert.AreEqual("Updated location", _viewModel.Incident.Location.Address);
    }

    /// <summary>
    /// Test 3: Saving without modifying any editable fields
    /// leaves the incident values unchanged.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_NoFieldsChanged_LeavesIncidentUnchanged()
    {
        Incident incident = await LoadCreatedIncidentAsync();
        await _viewModel.SaveChangesAsync();

        Assert.IsNotNull(_viewModel.Incident);
        Assert.AreEqual(incident.Status, _viewModel.Incident.Status);
        Assert.AreEqual(incident.Category, _viewModel.Incident.Category);
        Assert.AreEqual(incident.Severity, _viewModel.Incident.Severity);
        Assert.AreEqual(incident.Location.Latitude, _viewModel.Incident.Location.Latitude);
        Assert.AreEqual(incident.Location.Longitude, _viewModel.Incident.Location.Longitude);
        Assert.AreEqual(incident.Location.Address, _viewModel.Incident.Location.Address);
    }

    /// <summary>
    /// Test 4: SaveChangesAsync updates the location when only latitude changes.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_LatitudeChanged_UpdatesLocation()
    {
        await LoadCreatedIncidentAsync();
        _viewModel.EditableLatitude = 11.0;
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual(11.0, _viewModel.Incident!.Location.Latitude);
    }

    /// <summary>
    /// Test 5: SaveChangesAsync updates the location when only longitude changes.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_LongitudeChanged_UpdatesLocation()
    {
        await LoadCreatedIncidentAsync();
        _viewModel.EditableLongitude = 77.0;
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual(77.0, _viewModel.Incident!.Location.Longitude);
    }

    /// <summary>
    /// Test 6: SaveChangesAsync updates the location when only address changes.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_AddressChanged_UpdatesLocation()
    {
        await LoadCreatedIncidentAsync();
        _viewModel.EditableAddress = "Updated address";
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual("Updated address", _viewModel.Incident!.Location.Address);
    }

    /// <summary>
    /// Test 7: Successful SaveChangesAsync resets editable fields
    /// to the latest incident values.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_ResetsEditableFields()
    {
        await LoadCreatedIncidentAsync();

        _viewModel.EditableStatus = IncidentStatus.InProgress;
        _viewModel.EditableCategory = IncidentCategory.Theft;
        _viewModel.EditableSeverity = IncidentSeverity.Critical;
        _viewModel.EditableLatitude = 11.0;
        _viewModel.EditableLongitude = 77.0;
        _viewModel.EditableAddress = "Updated location";

        await _viewModel.SaveChangesAsync();

        Assert.AreEqual(_viewModel.Incident!.Status, _viewModel.EditableStatus);
        Assert.AreEqual(_viewModel.Incident.Category, _viewModel.EditableCategory);
        Assert.AreEqual(_viewModel.Incident.Severity, _viewModel.EditableSeverity);
        Assert.AreEqual(_viewModel.Incident.Location.Latitude, _viewModel.EditableLatitude);
        Assert.AreEqual(_viewModel.Incident.Location.Longitude, _viewModel.EditableLongitude);
        Assert.AreEqual(_viewModel.Incident.Location.Address, _viewModel.EditableAddress);
    }

    /// <summary>
    /// Test 8: Successful SaveChangesAsync disables edit mode.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_DisablesEditMode()
    {
        await LoadCreatedIncidentAsync();

        _viewModel.IsEditMode = true;

        await _viewModel.SaveChangesAsync();

        Assert.IsFalse(_viewModel.IsEditMode);
        Assert.IsTrue(_viewModel.IsReadOnlyMode);
    }

    /// <summary>
    /// Test 9: Successful SaveChangesAsync resets IsBusy.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_ResetsIsBusy()
    {
        await LoadCreatedIncidentAsync();
        await _viewModel.SaveChangesAsync();

        Assert.IsFalse(_viewModel.IsBusy);
        Assert.IsTrue(_viewModel.IsNotBusy);
    }

    /// <summary>
    /// Test 10: Successful SaveChangesAsync clears an existing error message.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_ClearsErrorMessage()
    {
        await LoadCreatedIncidentAsync();
        _viewModel.ErrorMessage = "Previous error.";
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);
    }

    /// <summary>
    /// Test 11: Successful SaveChangesAsync refreshes the change timeline.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_RefreshesChangeTimeline()
    {
        await LoadCreatedIncidentAsync();
        _viewModel.EditableStatus = IncidentStatus.InProgress;
        await _viewModel.SaveChangesAsync();

        Assert.IsNotEmpty(_viewModel.ChangeTimeline);
    }

    // AddEvidenceItemsAsync tests

    /// <summary>
    /// Test 12: AddEvidenceItemsAsync does nothing when no incident is loaded.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_NoIncident_DoesNothing()
    {
        List<IncidentEvidence> evidences = [];

        await _viewModel.AddEvidenceItemsAsync(evidences);

        Assert.IsNull(_viewModel.Incident);
        Assert.IsEmpty(_viewModel.Evidences);
        Assert.IsFalse(_viewModel.IsBusy);
    }

    /// <summary>
    /// Test 13: AddEvidenceItemsAsync does nothing when the evidence list is empty.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_EmptyEvidenceList_DoesNothing()
    {
        await LoadCreatedIncidentAsync();
        await _viewModel.AddEvidenceItemsAsync([]);

        Assert.IsEmpty(_viewModel.Evidences);
        Assert.IsFalse(_viewModel.IsBusy);
    }

    /// <summary>
    /// Test 14: AddEvidenceItemsAsync adds evidence through the real controller.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_AddsEvidence()
    {
        await LoadCreatedIncidentAsync();

        IncidentEvidence evidence = new(
            Guid.NewGuid(),
            "Photograph of the accident location",
            null,
            DateTime.UtcNow,
            _currentUserId);

        await _viewModel.AddEvidenceItemsAsync([evidence]);

        Assert.IsNotNull(_viewModel.Incident);
        Assert.HasCount(1, _viewModel.Evidences);
        Assert.AreEqual(evidence.Id, _viewModel.Evidences[0].Id);
        Assert.AreEqual(evidence.Description, _viewModel.Evidences[0].Description);
    }

    /// <summary>
    /// Test 15: Successful AddEvidenceItemsAsync refreshes the incident object.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_RefreshesIncident()
    {
        Incident originalIncident = await LoadCreatedIncidentAsync();

        IncidentEvidence evidence = new(
            Guid.NewGuid(),
            "Evidence description",
            null,
            DateTime.UtcNow,
            _currentUserId);

        await _viewModel.AddEvidenceItemsAsync([evidence]);

        Assert.IsNotNull(_viewModel.Incident);
        Assert.AreEqual(originalIncident.Id, _viewModel.Incident.Id);
        Assert.HasCount(1, _viewModel.Incident.Evidence);
    }

    /// <summary>
    /// Test 16: Successful AddEvidenceItemsAsync resets IsBusy.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_ResetsIsBusy()
    {
        await LoadCreatedIncidentAsync();

        IncidentEvidence evidence = new(
            Guid.NewGuid(),
            "Evidence description",
            null,
            DateTime.UtcNow,
            _currentUserId);

        await _viewModel.AddEvidenceItemsAsync([evidence]);

        Assert.IsFalse(_viewModel.IsBusy);
        Assert.IsTrue(_viewModel.IsNotBusy);
    }

    /// <summary>
    /// Test 17: Successful AddEvidenceItemsAsync clears an existing error message.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_ClearsErrorMessage()
    {
        await LoadCreatedIncidentAsync();

        _viewModel.ErrorMessage = "Previous error.";

        IncidentEvidence evidence = new(
            Guid.NewGuid(),
            "Evidence description",
            null,
            DateTime.UtcNow,
            _currentUserId);

        await _viewModel.AddEvidenceItemsAsync([evidence]);

        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);
    }

    // Helpers

    /// <summary>
    /// Creates an incident through the real IncidentController.
    /// </summary>
    private async Task<Incident> CreateIncidentAsync()
    {
        CreateIncidentRequest request = new(
            "Vehicle accident",
            "Test incident description",
            _currentUserId,
            IncidentCategory.Accident,
            IncidentSeverity.High,
            new IncidentLocation(10.0, 76.0, "Test location"),
            []);

        return await _controller.CreateIncident(request);
    }

    /// <summary>
    /// Creates an incident through the real controller and loads it into the ViewModel.
    /// </summary>
    private async Task<Incident> LoadCreatedIncidentAsync()
    {
        Incident incident = await CreateIncidentAsync();
        await _viewModel.LoadIncidentAsync(incident.Id);
        Assert.IsNotNull(_viewModel.Incident);
        return _viewModel.Incident;
    }
}
