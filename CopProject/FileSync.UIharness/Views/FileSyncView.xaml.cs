using Microsoft.UI.Xaml.Controls;

namespace FileSync.UIharness.Views;

/// <summary>
/// initial test view for filesync module
/// </summary>
public sealed partial class FileSyncView : UserControl
{
    private readonly FileSynchroniser.ViewModels.FileSyncViewModel _viewModel;

    /// <summary>
    /// creates a new instance of the FileSyncView
    /// </summary>
    public FileSyncView()
    {
        InitializeComponent();

        var synchroniser = new FileSynchroniser.Models.Synchroniser();
        _viewModel = new FileSynchroniser.ViewModels.FileSyncViewModel(synchroniser);

        RootFolderTextBlock.Text = $"Folder: {_viewModel.RootDirectory}";
        FilesListView.ItemsSource = _viewModel.Files;
    }

    /// <summary>
    /// handles click event on the sync button
    /// </summary>
    private void OnSyncButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string selectedPeer = (PeerSelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Default Peer";
        StatusTextBlock.Text = $"Synchronising with {selectedPeer}...";

        // trigger synchronisation on background task
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var synchroniser = new FileSynchroniser.Models.Synchroniser();
                synchroniser.Synchronise();

                DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.RefreshCommand.Execute(null);
                    StatusTextBlock.Text = $"Synchronisation with {selectedPeer} completed successfully!";
                });
            }
            catch (System.Exception ex)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusTextBlock.Text = $"Sync error: {ex.Message}";
                });
            }
        });
    }

    /// <summary>
    /// handles click event on the refresh button to reload file list
    /// </summary>
    private void OnRefreshClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.RefreshCommand.Execute(null);
        StatusTextBlock.Text = $"File list refreshed at {System.DateTime.Now:T}";
    }
}
