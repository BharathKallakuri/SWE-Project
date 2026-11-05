using System.Text.Json;
using Incident_module;
using LocationTracker.Interfaces;
using LocationTracker.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LocationTracker.Views;

public sealed partial class LocationMapView : UserControl
{
    public event Action<Guid>? IncidentSelected;

    private IMapService? _map;
    private LocationMapViewModel? _viewModel;
    private bool _filterOptionsInitialized;
    private bool _mapInitializationStarted;

    public LocationMapView()
    {
        InitializeComponent();
        Loaded += LocationMapView_Loaded;
    }

    public void Initialize(IMapService map)
    {
        _map = map;
        _viewModel = new LocationMapViewModel(map);
        if (IsLoaded)
        {
            _ = InitializeMapAsync();
        }
    }

    private async void LocationMapView_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeMapAsync();
    }

    private async Task InitializeMapAsync()
    {
        if (_map is null || _mapInitializationStarted || MapView.CoreWebView2 is not null)
        {
            return;
        }

        _mapInitializationStarted = true;
        MapView.NavigationCompleted += MapView_NavigationCompleted;
        // CoreWebView2 is initialized asynchronously; wait for it before navigating,
        // otherwise NavigateToString throws "A method was called at an unexpected time."
        await MapView.EnsureCoreWebView2Async();

        // OpenStreetMap blocks tile requests without a descriptive User-Agent.
        CoreWebView2 webView = MapView.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 failed to initialize.");
        webView.Settings.UserAgent =
            "CopProject-LocationTracker/1.0 (https://github.com/Software-Engineering-CS5617/project)";
        webView.WebMessageReceived += WebView_WebMessageReceived;

        MapView.NavigateToString(MapPage);
    }

    private void WebView_WebMessageReceived(
        CoreWebView2 sender,
        CoreWebView2WebMessageReceivedEventArgs args)
    {
        string messageJson = args.TryGetWebMessageAsString()
            ?? args.WebMessageAsJson;
        using JsonDocument message = JsonDocument.Parse(messageJson);
        if (!message.RootElement.TryGetProperty("type", out JsonElement type) ||
            type.GetString() != "incident-click" ||
            !message.RootElement.TryGetProperty("id", out JsonElement id) ||
            !Guid.TryParse(id.GetString(), out Guid incidentId))
        {
            return;
        }

        IncidentSelected?.Invoke(incidentId);
    }

    private async void MapView_NavigationCompleted(
        WebView2 sender,
        CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess || _map is null)
        {
            return;
        }

        await RefreshIncidentsAsync();
    }

    private IncidentFilter BuildFilter()
    {
        string? title = string.IsNullOrWhiteSpace(TitleFilterBox.Text)
            ? null : TitleFilterBox.Text;

        IncidentCategory? category = GetCategory();
        IncidentSeverity? severity = GetSeverity();
        IncidentStatus? status = GetStatus();

        DateTime? from = FromDatePicker.Date?.DateTime;
        DateTime? to = ToDatePicker.Date?.DateTime;

        return new IncidentFilter(title, status, category, severity, from, to, null, null);
    }

    private IncidentCategory? GetCategory()
    {
        string value = CategoryFilterBox.SelectedItem?.ToString() ?? "All";

        if (value != "All" && Enum.TryParse<IncidentCategory>(value, out var result))
        {
            return result;
        }

        return null;
    }

    private IncidentSeverity? GetSeverity()
    {
        string value = SeverityFilterBox.SelectedItem?.ToString() ?? "All";

        if (value != "All" && Enum.TryParse<IncidentSeverity>(value, out var result))
        {
            return result;
        }

        return null;
    }

    private IncidentStatus? GetStatus()
    {
        string value = StatusFilterBox.SelectedItem?.ToString() ?? "All";

        if (value != "All" && Enum.TryParse<IncidentStatus>(value, out var result))
        {
            return result;
        }

        return null;
    }

    private async void FilterChanged(object sender, object e)
    {
        if (_map is null || MapView.CoreWebView2 is null)
        {
            return;
        }

        await RefreshIncidentsAsync();
    }

    private async Task RefreshIncidentsAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.LoadAsync(BuildFilter());
        if (!_filterOptionsInitialized)
        {
            _filterOptionsInitialized = true;
            SetFilterOptions(_viewModel.Incidents);
        }

        string incidents = JsonSerializer.Serialize(_viewModel.Incidents);
        await MapView.ExecuteScriptAsync($"setIncidents({incidents});");
    }

    private void SetFilterOptions(IEnumerable<MapIncident> incidents)
    {
        CategoryFilterBox.ItemsSource = new[] { "All" }
            .Concat(incidents.Select(incident => incident.Category).Distinct());
        SeverityFilterBox.ItemsSource = new[] { "All" }
            .Concat(incidents.Select(incident => incident.Severity).Distinct());
        StatusFilterBox.ItemsSource = new[] { "All" }
            .Concat(incidents.Select(incident => incident.Status).Distinct());

        CategoryFilterBox.SelectedIndex = 0;
        SeverityFilterBox.SelectedIndex = 0;
        StatusFilterBox.SelectedIndex = 0;
    }

    private const string MapPage = """
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <link rel="stylesheet"
                href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
          <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
          <style>
            html, body, #map { height: 100%; width: 100%; margin: 0; }
            body { background: #dce8f2; }
          </style>
        </head>
        <body>
          <div id="map"></div>
          <script>
            const map = L.map('map').setView([10.79, 76.68], 12);
            const incidentMarkers = L.layerGroup().addTo(map);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
              attribution: '&copy; OpenStreetMap contributors'
            }).addTo(map);

            function setIncidents(incidents) {
              incidentMarkers.clearLayers();
              incidents.forEach(incident => {
                const popup = `<button onclick="selectIncident('${incident.Id}')">
                    <strong>${incident.Title}</strong></button><br>
                  ${incident.Category} · ${incident.Severity}<br>
                  ${incident.Address ?? ''}<br>${incident.Description}`;
                L.marker([incident.Latitude, incident.Longitude])
                  .addTo(incidentMarkers)
                  .bindPopup(popup);
              });
            }

            function selectIncident(id) {
              window.chrome.webview.postMessage(JSON.stringify({
                type: 'incident-click',
                id: id
              }));
            }
          </script>
        </body>
        </html>
        """;
}
