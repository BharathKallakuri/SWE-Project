using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incident_module.ViewModels;

public sealed class IncidentDetailsPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private readonly Guid _currentUserId;

    private Incident? _incident;
    private bool _isEditMode;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    // Editable properties
    private IncidentStatus _editableStatus;
    private IncidentCategory _editableCategory;
    private IncidentSeverity _editableSeverity;
    private double _editableLatitude;
    private double _editableLongitude;
    private string? _editableAddress;

    public IncidentDetailsPageViewModel(
        IIncidentController incidentController,
        Guid currentUserId)
    {
        _incidentController = incidentController ?? throw new ArgumentNullException(nameof(incidentController));
        _currentUserId = currentUserId;

        StatusOptions = Enum.GetValues(typeof(IncidentStatus)).Cast<IncidentStatus>().ToList();
        CategoryOptions = Enum.GetValues(typeof(IncidentCategory)).Cast<IncidentCategory>().ToList();
        SeverityOptions = Enum.GetValues(typeof(IncidentSeverity)).Cast<IncidentSeverity>().ToList();

        ToggleEditModeCommand = new RelayCommand(ToggleEditMode);
        CancelEditCommand = new RelayCommand(CancelEdit);
        SaveChangesCommand = new RelayCommand(async () => await SaveChangesAsync());
    }

    public Incident? Incident
    {
        get => _incident;
        private set {
            _incident = value;
            OnPropertyChanged(nameof(Incident));
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set {
            if (_isEditMode == value) { return; }
            _isEditMode = value;
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsReadOnlyMode));
            OnPropertyChanged(nameof(EditButtonText));
            OnPropertyChanged(nameof(EditVisibility));
            OnPropertyChanged(nameof(ReadOnlyVisibility));
        }
    }

    public bool IsReadOnlyMode => !IsEditMode;

    // Direct Visibility properties for XAML binding
    public Visibility EditVisibility => IsEditMode ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ReadOnlyVisibility => IsReadOnlyMode ? Visibility.Visible : Visibility.Collapsed;

    public string EditButtonText => IsEditMode ? "Editing..." : "Edit Incident";

    public bool IsBusy
    {
        get => _isBusy;
        set {
            if (_isBusy == value) { return; }
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    public string ErrorMessage
    {
        get => _errorMessage;
        set {
            if (_errorMessage == value) { return; }
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(ErrorVisibility));
        }
    }

    public Visibility ErrorVisibility =>
        string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

    // Editable bindings
    public IncidentStatus EditableStatus
    {
        get => _editableStatus;
        set {
            if (_editableStatus == value) { return; }
            _editableStatus = value;
            OnPropertyChanged(nameof(EditableStatus));
        }
    }

    public IncidentCategory EditableCategory
    {
        get => _editableCategory;
        set {
            if (_editableCategory == value) { return; }
            _editableCategory = value;
            OnPropertyChanged(nameof(EditableCategory));
        }
    }

    public IncidentSeverity EditableSeverity
    {
        get => _editableSeverity;
        set {
            if (_editableSeverity == value) { return; }
            _editableSeverity = value;
            OnPropertyChanged(nameof(EditableSeverity));
        }
    }

    public double EditableLatitude
    {
        get => _editableLatitude;
        set {
            if (_editableLatitude == value) { return; }
            _editableLatitude = value;
            OnPropertyChanged(nameof(EditableLatitude));
        }
    }

    public double EditableLongitude
    {
        get => _editableLongitude;
        set {
            if (_editableLongitude == value) { return; }
            _editableLongitude = value;
            OnPropertyChanged(nameof(EditableLongitude));
        }
    }

    public string? EditableAddress
    {
        get => _editableAddress;
        set {
            if (_editableAddress == value) { return; }
            _editableAddress = value;
            OnPropertyChanged(nameof(EditableAddress));
        }
    }

    public List<IncidentStatus> StatusOptions { get; }
    public List<IncidentCategory> CategoryOptions { get; }
    public List<IncidentSeverity> SeverityOptions { get; }

    public ObservableCollection<IncidentEvidence> Evidences { get; } = [];
    public ObservableCollection<IChange> ChangeTimeline { get; } = [];

    public RelayCommand ToggleEditModeCommand { get; }
    public RelayCommand CancelEditCommand { get; }
    public RelayCommand SaveChangesCommand { get; }

    public async Task LoadIncidentAsync(Guid incidentId)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            Incident = await _incidentController.GetIncidentById(incidentId);

            if (Incident != null)
            {
                ResetEditableFields();
                RefreshCollections();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load incident: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ToggleEditMode()
    {
        if (Incident == null) { return; }
        ResetEditableFields();
        IsEditMode = !IsEditMode;
    }

    public void CancelEdit()
    {
        ResetEditableFields();
        IsEditMode = false;
    }

    public async Task SaveChangesAsync()
    {
        if (Incident == null) { return; }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            // 1. Update Status if changed
            if (Incident.Status != EditableStatus)
            {
                await _incidentController.UpdateStatus(Incident, EditableStatus);
            }

            // 2. Update Category if changed
            if (Incident.Category != EditableCategory)
            {
                await _incidentController.UpdateCategory(Incident, EditableCategory);
            }

            // 3. Update Severity if changed
            if (Incident.Severity != EditableSeverity)
            {
                await _incidentController.UpdateSeverity(Incident, EditableSeverity);
            }

            // 4. Update Location if changed
            IncidentLocation newLocation = new(EditableLatitude, EditableLongitude, EditableAddress);
            if (Incident.Location.Latitude != EditableLatitude ||
                Incident.Location.Longitude != EditableLongitude ||
                Incident.Location.Address != EditableAddress)
            {
                await _incidentController.UpdateLocation(Incident, newLocation);
            }

            // Reload to sync latest incident object state and timeline
            Incident = await _incidentController.GetIncidentById(Incident.Id);
            ResetEditableFields();
            RefreshCollections();

            IsEditMode = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save changes: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AddEvidenceItemsAsync(List<IncidentEvidence> newEvidences)
    {
        if (Incident == null || newEvidences.Count == 0) { return; }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await _incidentController.AddEvidence(Incident, newEvidences);

            Incident = await _incidentController.GetIncidentById(Incident.Id);
            RefreshCollections();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add evidence: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetEditableFields()
    {
        if (Incident == null) { return; }

        EditableStatus = Incident.Status;
        EditableCategory = Incident.Category;
        EditableSeverity = Incident.Severity;
        EditableLatitude = Incident.Location.Latitude;
        EditableLongitude = Incident.Location.Longitude;
        EditableAddress = Incident.Location.Address;
    }

    private void RefreshCollections()
    {
        if (Incident == null) { return; }

        Evidences.Clear();
        foreach (IncidentEvidence evidence in Incident.Evidence)
        {
            Evidences.Add(evidence);
        }

        ChangeTimeline.Clear();
        // Display latest timeline events first
        foreach (IChange change in Incident.ChangeHistory.OrderByDescending(c => c.ChangedTime))
        {
            ChangeTimeline.Add(change);
        }
    }
}
