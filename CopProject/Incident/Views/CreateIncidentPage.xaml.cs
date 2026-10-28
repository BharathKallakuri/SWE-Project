using System;
using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incident_module.Views;

public sealed partial class CreateIncidentPage : Page
{
    public CreateIncidentPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid currentUserId)
        {
            DataContext = new CreateIncidentPageViewModel(
                AppServices.IncidentController,
                AppServices.NavigationService,
                currentUserId
            );
        }
    }
}
