using Microsoft.UI.Xaml.Controls;

namespace FileSync.UIharness.Views;

/// <summary>
/// initial test view for filesync module
/// </summary>
public sealed partial class FileSyncView : UserControl
{
    /// <summary>
    /// creates a new instance of the FileSyncView
    /// </summary>
    public FileSyncView()
    {
        InitializeComponent();
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
}
