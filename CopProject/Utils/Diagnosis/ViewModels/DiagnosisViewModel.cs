
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Utils.Diagnosis.Services;

namespace Utils.Diagnosis.ViewModels;

/// <summary>
/// Manages the diagnosis page's state and coordinates log analysis
/// through the Insights diagnosis service.
/// </summary>
public sealed class DiagnosisViewModel : INotifyPropertyChanged
{
    private readonly InsightsDiagnosisService _service;
    private string _status = "Ready to diagnose the recent app log.";
    private string _report = string.Empty;
    private bool _isBusy;

    /// <summary>
    /// Initializes the view model with the diagnosis service.
    /// </summary>

    public DiagnosisViewModel(InsightsDiagnosisService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Occurs when a view model property changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the current status message displayed on the diagnosis page.
    /// </summary>
    public string Status
    {
        get => _status;
        private set { _status = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets the diagnosis report returned by the Insights service.
    /// </summary>
    public string Report
    {
        get => _report;
        private set { _report = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets a value indicating whether a diagnosis operation is in progress.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set {
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRun));
        }
    }

    /// <summary>
    /// Gets a value indicating whether a new diagnosis can be started.
    /// </summary>
    public bool CanRun => !IsBusy;

    /// <summary>
    /// Reads the recent application log and requests a diagnosis
    /// from the Insights service.
    /// </summary>
    /// <remarks>
    /// Updates the status and report properties throughout the operation
    /// and handles any exceptions encountered during diagnosis.
    /// </remarks>
    public async Task RunAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        Status = "Analyzing log...";
        Report = string.Empty;

        try
        {
            Report = await _service.DiagnoseAsync();
            Status = "Diagnosis complete.";
        }
        catch (Exception ex)
        {
            Status = "Diagnosis failed.";
            Report = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
