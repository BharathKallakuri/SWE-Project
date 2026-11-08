using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incidents.ViewModels;

public sealed class AllIncidentsPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private readonly Guid _currentUserId;

    private List<Incident> _allIncidents = [];
    private string _searchText = string.Empty;

    // Dropdown selections default to "All"
    private string _selectedStatus = "All";
    private string _selectedCategory = "All";
    private string _selectedSeverity = "All";

    // Date filters
    private DateTimeOffset? _fromDate;
    private DateTimeOffset? _toDate;

    // User filter
    private string _createdByInput = string.Empty;

    public ObservableCollection<Incident> Incidents { get; } = [];

    // String collections with "All" explicitly added as option 0
    public List<string> StatusOptions { get; }
    public List<string> CategoryOptions { get; }
    public List<string> SeverityOptions { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public AllIncidentsPageViewModel(
        IIncidentController incidentController,
        Guid currentUserId)
    {
        _incidentController = incidentController ?? throw new ArgumentNullException(nameof(incidentController));
        _currentUserId = currentUserId;

        // Build string options list starting with "All"
        StatusOptions = ["All", .. Enum.GetNames<IncidentStatus>()];
        CategoryOptions = ["All", .. Enum.GetNames<IncidentCategory>()];
        SeverityOptions = ["All", .. Enum.GetNames<IncidentSeverity>()];

        ClearFiltersCommand = new RelayCommand(ClearFilters);
    }

    public string SearchText
    {
        get => _searchText;
        set {
            if (_searchText == value) { return; }
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            ApplyFilter();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set {
            if (_selectedStatus == value) { return; }
            _selectedStatus = value;
            OnPropertyChanged(nameof(SelectedStatus));
            ApplyFilter();
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set {
            if (_selectedCategory == value) { return; }
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
            ApplyFilter();
        }
    }

    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set {
            if (_selectedSeverity == value) { return; }
            _selectedSeverity = value;
            OnPropertyChanged(nameof(SelectedSeverity));
            ApplyFilter();
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

    public string CreatedByInput
    {
        get => _createdByInput;
        set {
            if (_createdByInput == value) { return; }
            _createdByInput = value;
            OnPropertyChanged(nameof(CreatedByInput));
            ApplyFilter();
        }
    }

    public Visibility EmptyMessageVisibility =>
        Incidents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public async Task LoadAsync()
    {
        _allIncidents = await _incidentController.GetAllIncidents();
        ApplyFilter();
    }

    public void ClearFilters()
    {
        _searchText = string.Empty;
        _selectedStatus = "All";
        _selectedCategory = "All";
        _selectedSeverity = "All";
        _fromDate = null;
        _toDate = null;
        _createdByInput = string.Empty;

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedSeverity));
        OnPropertyChanged(nameof(FromDate));
        OnPropertyChanged(nameof(ToDate));
        OnPropertyChanged(nameof(CreatedByInput));

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string search = SearchText.Trim();
        string? titlePattern = string.IsNullOrWhiteSpace(search) ? null : Regex.Escape(search);

        // Convert string selection to nullable enum ("All" will fail to parse and return null)
        IncidentStatus? statusFilter = Enum.TryParse<IncidentStatus>(SelectedStatus, out IncidentStatus s) ? s : null;
        IncidentCategory? categoryFilter = Enum.TryParse<IncidentCategory>(SelectedCategory, out IncidentCategory c) ? c : null;
        IncidentSeverity? severityFilter = Enum.TryParse<IncidentSeverity>(SelectedSeverity, out IncidentSeverity v) ? v : null;

        // Convert string to Guid if valid
        Guid? createdByGuid = Guid.TryParse(CreatedByInput.Trim(), out Guid parsedGuid) ? parsedGuid : null;

        IncidentFilter filter = new(
            Title: titlePattern,
            Status: statusFilter,
            Category: categoryFilter,
            Severity: severityFilter,
            FromDateTime: FromDate?.DateTime,
            ToDateTime: ToDate?.DateTime,
            CreatedBy: createdByGuid,
            Location: null
        );

        List<Incident> filteredIncidents = _allIncidents
            .Where(i => IncidentFilterHelper.Matches(i, filter))
            .OrderByDescending(i => i.UpdateTime)
            .ToList();

        Incidents.Clear();

        foreach (Incident incident in filteredIncidents)
        {
            Incidents.Add(incident);
        }

        OnPropertyChanged(nameof(EmptyMessageVisibility));
    }
}
