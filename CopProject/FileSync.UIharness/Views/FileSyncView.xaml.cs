using Microsoft.UI.Xaml.Controls;

namespace FileSync.UIharness.Views;

/// <summary>
/// initial test view for filesync module
/// </summary>
public sealed partial class FileSyncView : UserControl
{
    private readonly FileSynchroniser.ViewModels.FileSyncViewModel _viewModel;
    private FileSync.ConsoleApp.SimulatedCommunicator? _communicator;
    private FileSync.ConsoleApp.NetworkSyncHandler? _syncHandler;
    private int _currentPort = 0;

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
    /// ensures communicator is initialized on the configured listening port
    /// </summary>
    private void EnsureCommunicator(int listenPort)
    {
        if (_communicator != null && _currentPort == listenPort)
        {
            return;
        }

        _communicator?.Dispose();
        _communicator = new FileSync.ConsoleApp.SimulatedCommunicator(listenPort);
        _currentPort = listenPort;

        _syncHandler = new FileSync.ConsoleApp.NetworkSyncHandler(_viewModel.RootDirectory, _communicator);

        _syncHandler.OnLog += msg =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StatusTextBlock.Text = msg;
            });
        };

        _syncHandler.OnSyncCompleted += () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _viewModel.RefreshCommand.Execute(null);
                StatusTextBlock.Text = "[sync] synchronisation finished successfully!";
            });
        };
    }

    /// <summary>
    /// handles click event on the sync button
    /// </summary>
    private void OnSyncButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string peerIp = PeerIpTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(peerIp))
        {
            peerIp = "127.0.0.1";
        }

        if (!int.TryParse(MyPortTextBox.Text.Trim(), out int listenPort))
        {
            listenPort = 8001;
        }

        if (!int.TryParse(PeerPortTextBox.Text.Trim(), out int peerPort))
        {
            peerPort = 8002;
        }

        StatusTextBlock.Text = $"Connecting on port {listenPort} to {peerIp}:{peerPort}...";

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                EnsureCommunicator(listenPort);

                var peerEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(peerIp), peerPort);
                _communicator!.RegisterPeer(peerEndpoint);
                _syncHandler!.DefaultPeerEndpoint = peerEndpoint;

                _syncHandler.StartSync();
            }
            catch (System.Exception ex)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusTextBlock.Text = $"Network sync error: {ex.Message}";
                });
            }
        });
    }

    /// <summary>
    /// preset A button click: listen 8001, target 8002
    /// </summary>
    private void OnPresetAClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MyPortTextBox.Text = "8001";
        PeerIpTextBox.Text = "127.0.0.1";
        PeerPortTextBox.Text = "8002";
    }

    /// <summary>
    /// preset B button click: listen 8002, target 8001
    /// </summary>
    private void OnPresetBClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MyPortTextBox.Text = "8002";
        PeerIpTextBox.Text = "127.0.0.1";
        PeerPortTextBox.Text = "8001";
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
