
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incident_module.ViewModels;

internal sealed class AllIncidentsPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private List<Incident> _allIncidents = [];
    private string _searchText = string.Empty;
    public ObservableCollection<Incident> Incidents { get; } = [];
    public RelayCommand CreateIncidentCommand { get; }
    public event EventHandler? CreateIncidentRequested;

    public AllIncidentsPageViewModel(IIncidentController incidentController)
    {
        _incidentController = incidentController;

        CreateIncidentCommand = new RelayCommand(
            () => CreateIncidentRequested?.Invoke(this, EventArgs.Empty));
    }

    public string SearchText
    {
        get => _searchText;
        set {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
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

    private void ApplyFilter()
    {
        string search = SearchText.Trim();

        string? titlePattern = string.IsNullOrWhiteSpace(search)
            ? null
            : Regex.Escape(search);

        IncidentFilter filter = new(
            Title: titlePattern,
            Status: null,
            Category: null,
            Severity: null,
            FromDateTime: null,
            ToDateTime: null,
            CreatedBy: null,
            Location: null);

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
