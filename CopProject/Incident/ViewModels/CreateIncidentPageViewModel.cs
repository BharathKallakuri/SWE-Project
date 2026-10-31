using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incident_module.ViewModels;

public sealed class CreateIncidentPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private readonly Guid _currentUserId;
    public event EventHandler? IncidentCreated;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private IncidentCategory _selectedCategory;
    private IncidentSeverity _selectedSeverity;
    private double _latitude;
    private double _longitude;
    private string? _address;
    private string _errorMessage = string.Empty;
    private bool _isSubmitting;

    public CreateIncidentPageViewModel(
        IIncidentController incidentController,
        Guid currentUserId)
    {
        _incidentController = incidentController ?? throw new ArgumentNullException(nameof(incidentController));
        _currentUserId = currentUserId;

        Categories = Enum.GetValues(typeof(IncidentCategory)).Cast<IncidentCategory>().ToList();
        Severities = Enum.GetValues(typeof(IncidentSeverity)).Cast<IncidentSeverity>().ToList();

        if (Categories.Count > 0)
        {
            _selectedCategory = Categories[0];
        }

        if (Severities.Count > 0)
        {
            _selectedSeverity = Severities[0];
        }

        SubmitCommand = new RelayCommand(async () => await SubmitAsync());
    }

    public IReadOnlyList<IncidentCategory> Categories { get; }
    public IReadOnlyList<IncidentSeverity> Severities { get; }
    public ObservableCollection<IncidentEvidence> Evidences { get; } = [];

    public RelayCommand SubmitCommand { get; }

    public string Title
    {
        get => _title;
        set {
            if (_title == value) { return; }
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }

    public string Description
    {
        get => _description;
        set {
            if (_description == value) { return; }
            _description = value;
            OnPropertyChanged(nameof(Description));
        }
    }

    public IncidentCategory SelectedCategory
    {
        get => _selectedCategory;
        set {
            if (_selectedCategory == value) { return; }
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    public IncidentSeverity SelectedSeverity
    {
        get => _selectedSeverity;
        set {
            if (_selectedSeverity == value) { return; }
            _selectedSeverity = value;
            OnPropertyChanged(nameof(SelectedSeverity));
        }
    }

    public double Latitude
    {
        get => _latitude;
        set {
            if (_latitude == value) { return; }
            _latitude = value;
            OnPropertyChanged(nameof(Latitude));
        }
    }

    public double Longitude
    {
        get => _longitude;
        set {
            if (_longitude == value) { return; }
            _longitude = value;
            OnPropertyChanged(nameof(Longitude));
        }
    }

    public string? Address
    {
        get => _address;
        set {
            if (_address == value) { return; }
            _address = value;
            OnPropertyChanged(nameof(Address));
        }
    }

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

    public bool IsSubmitting
    {
        get => _isSubmitting;
        set {
            if (_isSubmitting == value) { return; }
            _isSubmitting = value;
            OnPropertyChanged(nameof(IsSubmitting));
            OnPropertyChanged(nameof(IsNotSubmitting));
        }
    }

    public bool IsNotSubmitting => !IsSubmitting;

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Description is required.";
            return;
        }

        if (Latitude < -90 || Latitude > 90)
        {
            ErrorMessage = "Latitude must be between -90 and 90.";
            return;
        }

        if (Longitude < -180 || Longitude > 180)
        {
            ErrorMessage = "Longitude must be between -180 and 180.";
            return;
        }

        try
        {
            IsSubmitting = true;
            ErrorMessage = string.Empty;

            IncidentLocation location = new(Latitude, Longitude, Address);

            CreateIncidentRequest request = new(
                Title.Trim(),
                Description.Trim(),
                _currentUserId,
                SelectedCategory,
                SelectedSeverity,
                location,
                Evidences.ToList()
            );

            await _incidentController.CreateIncident(request);

            IncidentCreated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create incident: {ex.Message}";
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
