using System;
using Incident_module;
using Incident_module.Implementations;
using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;

namespace Incidents.UIharness;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeAndStartAsync();
    }

    private async void InitializeAndStartAsync()
    {
        // Instantiate implementations
        IncidentCloudStorageService storageService = new();
        IncidentFileSynchroniser fileSynchroniser = new();

        // Initialize IncidentController asynchronously
        IIncidentController controller = await IncidentController.CreateAsync(storageService, fileSynchroniser);

        // Set up NavigationService with the RootFrame in MainWindow.xaml
        NavigationService navigationService = new();
        navigationService.Initialize(RootFrame);

        // Register shared services
        AppServices.IncidentController = controller;
        AppServices.NavigationService = navigationService;
        AppServices.CurrentUserId = Guid.NewGuid(); // Test user ID

        // Navigate to initial page
        navigationService.NavigateTo<AllIncidentsPageViewModel>();
    }
}
