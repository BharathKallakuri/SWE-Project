using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Incident_module.ViewModels.Utils;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Incident_module.ViewModels;

public sealed class AddEvidenceDialogViewModel : ViewModelBase
{
    private string _filePath = string.Empty;
    private string _description = string.Empty;
    private readonly IntPtr _windowHandle;

    public AddEvidenceDialogViewModel(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        BrowseFileCommand = new RelayCommand(async () => await BrowseFileAsync());
    }

    public RelayCommand BrowseFileCommand { get; }

    public string FilePath
    {
        get => _filePath;
        set {
            if (_filePath == value) { return; }
            _filePath = value;
            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    public string Description
    {
        get => _description;
        set {
            if (_description == value) { return; }
            _description = value;
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(Description);

    private async Task BrowseFileAsync()
    {
        try
        {
            if (_windowHandle == IntPtr.Zero)
            {
                Debug.WriteLine("AppServices.WindowHandle is IntPtr.Zero!");
                return;
            }

            FileOpenPicker picker = new();
            InitializeWithWindow.Initialize(picker, _windowHandle);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            picker.FileTypeFilter.Add("*");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                FilePath = file.Path;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open file picker: {ex.Message}");
        }
    }

    public IncidentEvidence BuildEvidence(Guid uploadedBy)
    {
        return new IncidentEvidence(
            Id: Guid.NewGuid(),
            Description: Description.Trim(),
            FilePath: FilePath,
            UploadTime: DateTime.Now,
            UploadedBy: uploadedBy
        );
    }
}
