using System;
using System.Diagnostics.CodeAnalysis;
using Incident_module.ViewModels;
using Incident_module.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incident_module.Views;

[ExcludeFromCodeCoverage]
public sealed partial class IncidentDetailsPage : Page
{
    private IncidentDetailsPageViewModel? ViewModel => DataContext as IncidentDetailsPageViewModel;
    private IncidentPageContext? _context;

    public IncidentDetailsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is IncidentDetailsPageParameters parameters)
        {
            _context = parameters.Context;

            if (DataContext is not IncidentDetailsPageViewModel)
            {
                DataContext = new IncidentDetailsPageViewModel(
                    _context.IncidentController,
                    _context.CurrentUserId
                );
            }

            if (ViewModel != null)
            {
                await ViewModel.LoadIncidentAsync(parameters.IncidentId);
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
        if (_context == null)
        {
            return;
        }

        AddEvidenceDialogViewModel dialogViewModel = new(_context.WindowHandle);

        AddEvidenceDialog dialog = new() {
            XamlRoot = this.XamlRoot,
            DataContext = dialogViewModel
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialogViewModel.IsValid && ViewModel != null)
        {
            IncidentEvidence evidence = dialogViewModel.BuildEvidence(_context.CurrentUserId);

            await ViewModel.AddEvidenceItemsAsync([evidence]);
        }
    }
}
