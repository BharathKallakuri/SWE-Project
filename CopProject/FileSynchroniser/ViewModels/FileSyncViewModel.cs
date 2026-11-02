using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileSynchroniser.Models;
using Microsoft.UI.Dispatching;

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Display model for a single file in the synchronised root directory.
/// </summary>
public class SyncFileItem
{
    public string RelativePath { get; init; } = string.Empty;
    public string SizeText { get; init; } = string.Empty;
    public string LastModifiedText { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel for the synchroniser view.
/// </summary>
public class FileSyncViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISync _sync;
    private readonly IFileOperations _files;
    private readonly DispatcherQueue? _dispatcher;
    private readonly RelayCommand _deleteSelectedCommand;

    private SyncFileItem? _selectedFile;
    private string _statusMessage = "Ready";
    private bool _hasError;

    // Synchroniser is public; ISync is internal, so the constructor takes the concrete type.
    public FileSyncViewModel(Synchroniser synchroniser)
    {
        ArgumentNullException.ThrowIfNull(synchroniser);

        _sync = synchroniser;
        _files = synchroniser;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        RefreshCommand = new RelayCommand(Refresh);
        _deleteSelectedCommand = new RelayCommand(DeleteSelected, CanDeleteSelected);

        _sync.OnSyncComplete += HandleSyncComplete;
        _sync.OnSyncError += HandleSyncError;

        RootDirectory = _files.GetDirectory();
        LoadFiles();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RootDirectory { get; }

    public ObservableCollection<SyncFileItem> Files { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand DeleteSelectedCommand => _deleteSelectedCommand;

    public SyncFileItem? SelectedFile
    {
        get => _selectedFile;
        set {
            if (SetProperty(ref _selectedFile, value))
            {
                _deleteSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage; private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasError
    {
        get => _hasError; private set => SetProperty(ref _hasError, value);
    }

    private void Refresh()
    {
        HasError = false;
        _sync.Synchronise();   // raises OnSyncComplete / OnSyncError
        LoadFiles();
    }

    private void DeleteSelected()
    {
        if (SelectedFile is null)
        {
            return;
        }

        string path = SelectedFile.RelativePath;
        HasError = false;

        if (_files.DeleteFile(path))
        {
            StatusMessage = $"Deleted '{path}'.";
            LoadFiles();
        }
        // On failure, Synchroniser raises OnSyncError, which updates the status.
    }

    private bool CanDeleteSelected()
    {
        return SelectedFile is not null;
    }

    /// <summary>
    /// Called by the view after the user picks a file.
    /// </summary>
    public void AddFile(string relativePath, byte[] content)
    {
        HasError = false;

        if (_files.SaveFile(relativePath, content))
        {
            StatusMessage = $"Saved '{relativePath}'.";
            LoadFiles();
        }
    }

    /// <summary>
    /// Reads the folder listing into <see cref="Files"/>.
    /// </summary>
    private void LoadFiles()
    {
        Files.Clear();
        SelectedFile = null;

        try
        {
            IEnumerable<FileInfo> infos = new DirectoryInfo(RootDirectory)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .OrderBy(f => f.FullName, StringComparer.OrdinalIgnoreCase);

            foreach (FileInfo info in infos)
            {
                Files.Add(new SyncFileItem {
                    RelativePath = Path.GetRelativePath(RootDirectory, info.FullName),
                    SizeText = FormatSize(info.Length),
                    LastModifiedText = info.LastWriteTime.ToString("g")
                });
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError(ex.Message);
        }
    }

    private void HandleSyncComplete()
    {
        RunOnUi(() => StatusMessage = $"Synchronised at {DateTime.Now:T}.");
    }

    private void HandleSyncError(string message)
    {
        RunOnUi(() => ShowError(message));
    }

    private void ShowError(string message)
    {
        HasError = true;
        StatusMessage = message;
    }

    // Events may be raised from any thread, so marshal back to the UI thread.
    private void RunOnUi(Action action)
    {
        if (_dispatcher is null || _dispatcher.HasThreadAccess)
        {
            action();
        }
        else
        {
            _dispatcher.TryEnqueue(() => action());
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{size:0.##} {units[unit]}";
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void Dispose()
    {
        _sync.OnSyncComplete -= HandleSyncComplete;
        _sync.OnSyncError -= HandleSyncError;
    }
}
