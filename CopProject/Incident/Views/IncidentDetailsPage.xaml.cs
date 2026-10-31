using System;
using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incident_module.Views;

public sealed partial class IncidentDetailsPage : Page
{
    private IncidentDetailsPageViewModel? ViewModel => DataContext as IncidentDetailsPageViewModel;

    public IncidentDetailsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid incidentId)
        {
            if (DataContext is not IncidentDetailsPageViewModel)
            {
                DataContext = new IncidentDetailsPageViewModel(
                    AppServices.IncidentController,
                    AppServices.CurrentUserId
                );
            }

            if (ViewModel != null)
            {
                await ViewModel.LoadIncidentAsync(incidentId);
            }
        }
    }

    private void BackButton_Click(object _, RoutedEventArgs __)
    {
        if (this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }

    private async void AddEvidence_Click(object _, RoutedEventArgs __)
    {
        AddEvidenceDialogViewModel dialogViewModel = new();

        AddEvidenceDialog dialog = new AddEvidenceDialog {
            XamlRoot = this.XamlRoot,
            DataContext = dialogViewModel
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary &&
            dialogViewModel.IsValid)
        {
            IncidentEvidence evidence = dialogViewModel.BuildEvidence(AppServices.CurrentUserId);

            if (ViewModel != null)
            {
                await ViewModel.AddEvidenceItemsAsync([evidence]);
            }
        }
    }
}
