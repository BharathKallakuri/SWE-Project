using System.Diagnostics.CodeAnalysis;
using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;

namespace Incident_module.Views;

[ExcludeFromCodeCoverage]
public sealed partial class AllIncidentsPage : Page
{
    private AllIncidentsPageViewModel? ViewModel => DataContext as AllIncidentsPageViewModel;
    private IncidentPageContext? _context;

    public AllIncidentsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IncidentPageContext context)
        {
            _context = context;
        }

        if (_context == null)
        {
            return;
        }

        if (DataContext is not AllIncidentsPageViewModel)
        {
            DataContext = new AllIncidentsPageViewModel(
                _context.IncidentController,
                _context.CurrentUserId
            );
        }

        if (ViewModel != null)
        {
            await ViewModel.LoadAsync();
        }
    }

    private void CreateIncident_Click(object _, RoutedEventArgs __)
    {
        if (_context != null)
        {
            this.Frame.Navigate(typeof(CreateIncidentPage), _context);
        }
    }

    private void Incident_ItemClick(object _, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Incident incident && _context != null)
        {
            this.Frame.Navigate(
                typeof(IncidentDetailsPage),
                new IncidentDetailsPageParameters(_context, incident.Id)
            );
        }
    }
}
