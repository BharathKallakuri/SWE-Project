using System;
using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incident_module.Views;

public sealed partial class CreateIncidentPage : Page
{
    public CreateIncidentPageViewModel? ViewModel => DataContext as CreateIncidentPageViewModel;

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

    private async void AddEvidence_Click(object _, RoutedEventArgs __)
    {
        AddEvidenceDialogViewModel dialogViewModel = new();

        AddEvidenceDialog dialog = new() {
            XamlRoot = this.XamlRoot, // Use Page's XamlRoot
            DataContext = dialogViewModel
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialogViewModel.IsValid && ViewModel != null)
        {
            IncidentEvidence evidence = dialogViewModel.BuildEvidence(AppServices.CurrentUserId);
            ViewModel.Evidences.Add(evidence);
        }
    }
}
