using System.Diagnostics.CodeAnalysis;
using FileSynchroniser.Models;
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
    /// Disposes the ViewModel when the user navigates away from the page.
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Unsubscribe from the synchroniser events.
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
