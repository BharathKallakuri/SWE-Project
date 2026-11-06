// Author : Murali

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileSynchroniser.Models;

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
    private readonly Action<Action> _runOnUi;
    private readonly Func<string, IEnumerable<FileInfo>> _enumerateFiles;
    private readonly RelayCommand _deleteSelectedCommand;

    private SyncFileItem? _selectedFile;
    private string _statusMessage = "Ready";
    private bool _hasError;

    /// <summary>
    /// Production constructor. Captures the current UI thread's dispatcher when one is available,
    /// otherwise falls back to running callbacks inline (e.g. in unit tests).
    /// </summary>
    // Synchroniser is public; ISync is internal, so the constructor takes the concrete type.
    public FileSyncViewModel(Synchroniser synchroniser) : this(synchroniser, UiDispatcher.Create())
    {
    }

    /// <summary>
    /// Testable constructor. <paramref name="runOnUi"/> is used to marshal events back to the UI thread;
    /// when null, actions are run inline on the calling thread.
    /// <paramref name="enumerateFiles"/> lists every file under the given root directory;
    /// when null, the real file system is used.
    /// </summary>
    public FileSyncViewModel(Synchroniser synchroniser, Action<Action>? runOnUi,
        Func<string, IEnumerable<FileInfo>>? enumerateFiles = null)
    {
        ArgumentNullException.ThrowIfNull(synchroniser);

        _sync = synchroniser;
        _files = synchroniser;
        _runOnUi = runOnUi ?? (action => action());
        _enumerateFiles = enumerateFiles ?? EnumerateAllFiles;

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
            IEnumerable<FileInfo> infos = _enumerateFiles(RootDirectory)
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
        _runOnUi(action);
    }

    private static IEnumerable<FileInfo> EnumerateAllFiles(string root)
    {
        return new DirectoryInfo(root).EnumerateFiles("*", SearchOption.AllDirectories);
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
