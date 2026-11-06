using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using FileSynchroniser.Models;
using FileSynchroniser.Network;
using FileSynchroniser.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FileSynchroniser.Views;

[ExcludeFromCodeCoverage]
/// <summary>
/// Page that lists the synchronised folder and lets the user add, delete and refresh files.
/// </summary>
public sealed partial class FileSyncPage : Page
{
    /// <summary>
    /// ViewModel that the page binds to.
    /// </summary>
    public FileSyncViewModel ViewModel { get; }

    private SimulatedCommunicator? _communicator;
    private NetworkSyncHandler? _syncHandler;
    private int _currentPort = 0;

    /// <summary>
    /// Creates the page and its ViewModel.
    /// </summary>
    public FileSyncPage()
    {
        // Created here (on the UI thread) so UiDispatcher can capture the DispatcherQueue.
        ViewModel = new FileSyncViewModel(new Synchroniser());

        InitializeComponent();
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
        _communicator = new SimulatedCommunicator(listenPort);
        _currentPort = listenPort;

        _syncHandler = new NetworkSyncHandler(ViewModel.RootDirectory, _communicator);

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
                ViewModel.RefreshCommand.Execute(null);
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
            catch (Exception ex)
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
    /// Disposes the ViewModel when the user navigates away from the page.
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _communicator?.Dispose();
        ViewModel.Dispose();
        base.OnNavigatedFrom(e);
    }

    /// <summary>
    /// Opens a file picker and adds the chosen file to the synchronised folder.
    /// </summary>
    /// <param name="sender">The Add File button.</param>
    /// <param name="e">Event data for the click.</param>
    private async void AddFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");

            // WinUI 3 pickers must be tied to the window handle.
            IntPtr hwnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
            InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return; // user cancelled
            }

            byte[] content = await File.ReadAllBytesAsync(file.Path);

            // The ViewModel reports success/failure through its status message.
            ViewModel.AddFile(file.Name, content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowMessageAsync("Could not read the selected file", ex.Message);
        }
    }

    /// <summary>
    /// Shows a simple message dialog with an OK button.
    /// </summary>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="message">Message text shown in the dialog.</param>
    /// <returns>A task that completes when the dialog is closed.</returns>
    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}
