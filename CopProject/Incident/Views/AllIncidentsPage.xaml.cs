using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;

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
                AppServices.CurrentUserId
            );
        }

        if (ViewModel != null)
        {
            await ViewModel.LoadAsync();
        }
    }

    private void CreateIncident_Click(object _, RoutedEventArgs __)
    {
        this.Frame.Navigate(typeof(CreateIncidentPage), AppServices.CurrentUserId);
    }

    private void Incident_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Incident incident)
        {
            this.Frame.Navigate(typeof(IncidentDetailsPage), incident.Id);
        }
    }
}
