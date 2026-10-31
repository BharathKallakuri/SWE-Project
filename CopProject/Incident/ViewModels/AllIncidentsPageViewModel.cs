using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incident_module.ViewModels;

public sealed class AllIncidentsPageViewModel : ViewModelBase
{
    private readonly IIncidentController _incidentController;
    private readonly Guid _currentUserId;

    private List<Incident> _allIncidents = [];
    private string _searchText = string.Empty;

    public ObservableCollection<Incident> Incidents { get; } = [];

    public AllIncidentsPageViewModel(
        IIncidentController incidentController,
        Guid currentUserId)
    {
        _incidentController = incidentController ?? throw new ArgumentNullException(nameof(incidentController));
        _currentUserId = currentUserId;
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
