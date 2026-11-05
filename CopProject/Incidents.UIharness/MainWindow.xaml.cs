using System;
using Incident_module;
using Incident_module.Implementations;
using Incident_module.Views;
using Cloud.Persistence;
using FileSynchroniser.Models;
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
        IStorage storageService = await StorageFactory.CreateAsync(ModuleType.IncidentManagement);
        IFileOperations fileSynchroniser = new Synchroniser();

        IIncidentController controller = await IncidentController.CreateAsync(storageService, fileSynchroniser);
        Guid currentUserId = Guid.NewGuid();
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        IncidentPageContext context = new(controller, currentUserId, windowHandle);
        RootFrame.Navigate(typeof(AllIncidentsPage), context);

    }
}
