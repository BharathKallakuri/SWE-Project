// Author = Vishesh Srivastava
// Roll Number = 112301037

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Incidents;
using Incidents.ViewModels.Utils;
using LocationTracker.Interfaces;
using LocationTracker.Models;
using LocationViewModelBase = LocationTracker.ViewModels.Utils.ViewModelBase;

namespace LocationTracker.ViewModels;

public sealed class LocationMapViewModel : LocationViewModelBase
{
    private readonly IMapService _mapService;
    private string _titleText = string.Empty;
    private string _selectedCategory = "All";
    private string _selectedSeverity = "All";
    private string _selectedStatus = "All";
    private DateTimeOffset? _fromDate;
    private DateTimeOffset? _toDate;
    private string _addressSearchText = string.Empty;
    private string _errorMessage = string.Empty;

    public ObservableCollection<MapIncident> Incidents { get; } = [];

    public event EventHandler? IncidentsChanged;
    public event EventHandler<LocationSearchResult>? LocationSearchRequested;

    public LocationMapViewModel(IMapService mapService)
    {
        _mapService = mapService;
        SearchAddressCommand = new RelayCommand(async () => await SearchAddressAsync());

        CategoryOptions = ["All", .. Enum.GetNames<IncidentCategory>()];
        SeverityOptions = ["All", .. Enum.GetNames<IncidentSeverity>()];
        StatusOptions = ["All", .. Enum.GetNames<IncidentStatus>()];
    }

    public List<string> CategoryOptions { get; }
    public List<string> SeverityOptions { get; }
    public List<string> StatusOptions { get; }
    public RelayCommand SearchAddressCommand { get; }

    public string AddressSearchText
    {
        get => _addressSearchText;
        set {
            if (_addressSearchText == value) { return; }
            _addressSearchText = value;
            OnPropertyChanged(nameof(AddressSearchText));
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set {
            if (_errorMessage == value) { return; }
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
        }
    }

    private string _networkErrorMessage = string.Empty;

    public string NetworkErrorMessage
    {
        get => _networkErrorMessage;
        private set {
            if (_networkErrorMessage == value) { return; }
            _networkErrorMessage = value;
            OnPropertyChanged(nameof(NetworkErrorMessage));
        }
    }

    /// <summary>
    /// Shows or clears a map tile/network error reported from the WebView.
    /// </summary>
    public void SetNetworkError(string message)
    {
        NetworkErrorMessage = message;
    }

    public string TitleText
    {
        get => _titleText;
        set {
            if (_titleText == value) { return; }
            _titleText = value;
            OnPropertyChanged(nameof(TitleText));
            _ = LoadAsync();
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set {
            if (_selectedCategory == value) { return; }
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
            _ = LoadAsync();
        }
    }

    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set {
            if (_selectedSeverity == value) { return; }
            _selectedSeverity = value;
            OnPropertyChanged(nameof(SelectedSeverity));
            _ = LoadAsync();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set {
            if (_selectedStatus == value) { return; }
            _selectedStatus = value;
            OnPropertyChanged(nameof(SelectedStatus));
            _ = LoadAsync();
        }
    }

    public DateTimeOffset? FromDate
    {
        get => _fromDate;
        set {
            if (_fromDate == value) { return; }
            _fromDate = value;
            OnPropertyChanged(nameof(FromDate));
            ApplyFilter();
        }
    }

    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set {
            if (_toDate == value) { return; }
            _toDate = value;
            OnPropertyChanged(nameof(ToDate));
            ApplyFilter();
        }
    }

    /// <summary>
    /// Loads incidents using the current map filters.
    /// </summary>
    public async Task LoadAsync()
    {
        if (!IsDateRangeValid())
        {
            return;
        }

        ErrorMessage = string.Empty;
        List<Incident> incidents;
        try
        {
            incidents = await _mapService.GetIncidentsForMapAsync(BuildFilter());
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Location] LocationMapViewModel.LoadAsync: Error occurred while loading incidents: {ex.Message}");
            ErrorMessage = "Could not load incidents.";
            return;
        }

        Incidents.Clear();
        foreach (Incident incident in incidents)
        {
            Incidents.Add(new MapIncident(
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Category.ToString(),
                incident.Severity.ToString(),
                incident.Status.ToString(),
                incident.Location.Latitude,
                incident.Location.Longitude,
                incident.Location.Address));
        }
        Trace.TraceInformation($"[Location] LocationMapViewModel.LoadAsync: Loaded {Incidents.Count} incidents.");
        IncidentsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resolves the entered address and sends it to the map.
    /// </summary>
    private async Task SearchAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(AddressSearchText))
        {
            ErrorMessage = "Enter an address to search.";
            return;
        }

        try
        {
            LocationSearchResult? location =
                await _mapService.SearchAddressAsync(AddressSearchText.Trim());
            if (location is null)
            {
                ErrorMessage = "Address not found.";
                return;
            }

            ErrorMessage = string.Empty;
            LocationSearchRequested?.Invoke(this, location);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Location] LocationMapViewModel.SearchAddressAsync: Address search failed: {ex.Message}");
            ErrorMessage = "Could not search for the address.";
        }
    }

    /// <summary>
    /// Converts the selected UI values into a service filter.
    /// </summary>
    private IncidentFilter BuildFilter()
    {
        string search = TitleText.Trim();
        string? title = string.IsNullOrEmpty(search) ? null : Regex.Escape(search);

        IncidentCategory? category =
            Enum.TryParse(SelectedCategory, out IncidentCategory categoryValue)
                ? categoryValue
                : null;
        IncidentSeverity? severity =
            Enum.TryParse(SelectedSeverity, out IncidentSeverity severityValue)
                ? severityValue
                : null;
        IncidentStatus? status =
            Enum.TryParse(SelectedStatus, out IncidentStatus statusValue)
                ? statusValue
                : null;

        return new IncidentFilter(
            title,
            status,
            category,
            severity,
            FromDate?.DateTime,
            ToDate?.DateTime,
            null,
            null);
    }

    /// <summary>
    /// Stops the reload when the selected dates are reversed.
    /// </summary>
    private void ApplyFilter()
    {
        if (!IsDateRangeValid())
        {
            Trace.TraceWarning($"[Location] LocationMapViewModel.ApplyFilter: Invalid date range FromDate={FromDate} ToDate={ToDate}.");
            ErrorMessage = "The start date must be before the end date.";
            return;
        }

        _ = LoadAsync();
    }

    private bool IsDateRangeValid()
    {
        return !FromDate.HasValue ||
               !ToDate.HasValue ||
               FromDate <= ToDate;
    }
}
