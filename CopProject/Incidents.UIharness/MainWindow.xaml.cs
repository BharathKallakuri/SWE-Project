using System;
using Incident_module;
using Incident_module.Implementations;
using Incident_module.ViewModels.Utils;
using Incident_module.Views;
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
        // Instantiate services/controllers
        IncidentCloudStorageService storageService = new();
        IncidentFileSynchroniser fileSynchroniser = new();

        IIncidentController controller = await IncidentController.CreateAsync(storageService, fileSynchroniser);

        // Register shared services 
        // Acts as a dependency factory: pulls global singletons from AppServices 
        // and injects them into the ViewModel via explicit constructor arguments.
        AppServices.IncidentController = controller;
        AppServices.CurrentUserId = Guid.NewGuid(); // Test user ID
        AppServices.WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Navigate directly to the initial View via Frame
        RootFrame.Navigate(typeof(AllIncidentsPage));
    }
}
