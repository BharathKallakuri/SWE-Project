using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UX.ViewModels;

namespace UX.Views;

public sealed partial class JoinDialog : ContentDialog
{
    public DashboardViewModel ViewModel { get; }

    private CancellationTokenSource? _cts;
    
    public JoinDialog(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

     async void Join_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        JoinButton.IsEnabled = false;
        bool success = await ViewModel.TryJoinAsync(_cts.Token);
        JoinButton.IsEnabled = true;
        if (success)
        {
            this.Hide(); // Close dialog on success and transition to meeting
        }
    }
    void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();          // Tell background thread to abandon connection
        ViewModel.ClearJoinForm();
        this.Hide();             // IMMEDIATELY close dialog and return to home page!
    }
}
