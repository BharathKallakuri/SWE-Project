using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Incidents;
using Incidents.Test.Implementations;
using Incidents.ViewModels;
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
    /// SaveChangesAsync does nothing when no incident is loaded.
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
    /// Saving changed status, category, severity, and location
    /// updates the incident through the real controller.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_AllFieldsChanged_UpdatesIncident()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

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
    /// Saving without modifying any editable fields
    /// leaves the incident values unchanged.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_NoFieldsChanged_LeavesIncidentUnchanged()
    {
        Incident incident = await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
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
    /// SaveChangesAsync updates the location when only latitude changes.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_LatitudeChanged_UpdatesLocation()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
        _viewModel.EditableLatitude = 11.0;
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual(11.0, _viewModel.Incident!.Location.Latitude);
    }

    /// <summary>
    /// SaveChangesAsync updates the location when only longitude changes.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_LongitudeChanged_UpdatesLocation()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
        _viewModel.EditableLongitude = 77.0;
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual(77.0, _viewModel.Incident!.Location.Longitude);
    }

    /// <summary>
    /// SaveChangesAsync updates the location when only address changes.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_AddressChanged_UpdatesLocation()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
        _viewModel.EditableAddress = "Updated address";
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual("Updated address", _viewModel.Incident!.Location.Address);
    }

    /// <summary>
    /// Successful SaveChangesAsync resets editable fields
    /// to the latest incident values.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_ResetsEditableFields()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

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
    /// Successful SaveChangesAsync disables edit mode.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_DisablesEditMode()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

        _viewModel.IsEditMode = true;

        await _viewModel.SaveChangesAsync();

        Assert.IsFalse(_viewModel.IsEditMode);
        Assert.IsTrue(_viewModel.IsReadOnlyMode);
    }

    /// <summary>
    /// Successful SaveChangesAsync resets IsBusy.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_ResetsIsBusy()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
        await _viewModel.SaveChangesAsync();

        Assert.IsFalse(_viewModel.IsBusy);
        Assert.IsTrue(_viewModel.IsNotBusy);
    }

    /// <summary>
    /// Successful SaveChangesAsync clears an existing error message.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_ClearsErrorMessage()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
        _viewModel.ErrorMessage = "Previous error.";
        await _viewModel.SaveChangesAsync();

        Assert.AreEqual(string.Empty, _viewModel.ErrorMessage);
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ErrorVisibility);
    }

    /// <summary>
    /// Successful SaveChangesAsync refreshes the change timeline.
    /// </summary>
    [TestMethod]
    public async Task SaveChangesAsync_Success_RefreshesChangeTimeline()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
        _viewModel.EditableStatus = IncidentStatus.InProgress;
        await _viewModel.SaveChangesAsync();

        Assert.IsNotEmpty(_viewModel.ChangeTimeline);
    }

    // AddEvidenceItemsAsync tests

    /// <summary>
    /// AddEvidenceItemsAsync does nothing when no incident is loaded.
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
    /// AddEvidenceItemsAsync does nothing when the evidence list is empty.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_EmptyEvidenceList_DoesNothing()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);
        await _viewModel.AddEvidenceItemsAsync([]);

        Assert.IsEmpty(_viewModel.Evidences);
        Assert.IsFalse(_viewModel.IsBusy);
    }

    /// <summary>
    /// AddEvidenceItemsAsync adds evidence through the real controller.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_AddsEvidence()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

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
    /// Successful AddEvidenceItemsAsync refreshes the incident object.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_RefreshesIncident()
    {
        Incident originalIncident = await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

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
    /// Successful AddEvidenceItemsAsync resets IsBusy.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_ResetsIsBusy()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

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
    /// Successful AddEvidenceItemsAsync clears an existing error message.
    /// </summary>
    [TestMethod]
    public async Task AddEvidenceItemsAsync_ValidEvidence_ClearsErrorMessage()
    {
        await VMTestHelpers.LoadCreatedIncidentAsync(_controller, _viewModel, _currentUserId);

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

}
