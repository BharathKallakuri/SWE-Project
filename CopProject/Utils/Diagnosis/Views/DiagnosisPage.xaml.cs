using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Utils.Diagnosis.Services;
using Utils.Diagnosis.ViewModels;

namespace Utils.Diagnosis.Views;

/// <summary>
/// Displays the diagnosis page and coordinates log diagnosis through the view model.
/// </summary>
public sealed partial class DiagnosisPage : Page
{
    /// <summary>
    /// Gets the view model responsible for managing the diagnosis operation and its UI state.
    /// </summary>
    public DiagnosisViewModel ViewModel { get; }

    /// <summary>
    /// Initializes the diagnosis page and sets up its data context.
    /// </summary>
    public DiagnosisPage()
    {
        ViewModel = new DiagnosisViewModel(new InsightsDiagnosisService());
        InitializeComponent();
        DataContext = ViewModel;
    }

    /// <summary>
    /// Handles the Run Diagnosis button click and starts the diagnosis process.
    /// </summary>
    private async void RunDiagnosis_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunAsync();
    }
}
