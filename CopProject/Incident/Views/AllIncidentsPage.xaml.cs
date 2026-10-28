using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incident_module.Views;

public sealed partial class AllIncidentsPage : Page
{
    private AllIncidentsPageViewModel? ViewModel => DataContext as AllIncidentsPageViewModel;

    public AllIncidentsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (DataContext is not AllIncidentsPageViewModel)
        {
            DataContext = new AllIncidentsPageViewModel(
                AppServices.IncidentController,
                AppServices.NavigationService,
                AppServices.CurrentUserId
            );
        }

        if (ViewModel != null)
        {
            await ViewModel.LoadAsync();
        }
    }
}
