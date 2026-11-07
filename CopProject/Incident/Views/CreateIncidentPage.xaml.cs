using System;
using System.Diagnostics.CodeAnalysis;
using Incidents.ViewModels;
using Incidents.ViewModels.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Incidents.Views;

[ExcludeFromCodeCoverage]
public sealed partial class CreateIncidentPage : Page
{
    public CreateIncidentPageViewModel? ViewModel => DataContext as CreateIncidentPageViewModel;
    private IncidentPageContext? _context;

    public CreateIncidentPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        IncidentCreatePageWithLocation? locationContext =
            e.Parameter as IncidentCreatePageWithLocation;

        _context = locationContext?.Context
            ?? e.Parameter as IncidentPageContext;

        if (_context == null) { return; }

        if (DataContext is not CreateIncidentPageViewModel)
        {
            var vm = new CreateIncidentPageViewModel(
                _context.IncidentController,
                _context.CurrentUserId
            );

            vm.IncidentCreated += (s, args) => {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            };

            DataContext = vm;
        }

        if (locationContext != null && ViewModel != null)
        {
            ViewModel.Latitude = locationContext.Latitude;
            ViewModel.Longitude = locationContext.Longitude;
            ViewModel.Address = locationContext.Address;
        }
    }

    private void CancelButton_Click(object _, RoutedEventArgs __)
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
            XamlRoot = this.XamlRoot, // Use Page's XamlRoot
            DataContext = dialogViewModel
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialogViewModel.IsValid && ViewModel != null)
        {
            //todo(@sreenath): Add evidence as well as bytes of the file uploaded
            IncidentEvidence evidence = dialogViewModel.BuildEvidence(_context.CurrentUserId);
            ViewModel.Evidences.Add(evidence);
        }
    }
}
