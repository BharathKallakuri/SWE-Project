// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Controller.ViewModels;
// Base class for every ViewModel.
// used as change preperties followed by calling refresh and whole screen updates
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // updating everything on the screen that is bound to this ViewModel
    protected void Refresh()
    {
        Trace.TraceInformation("[Controller]: Refreshing all properties.");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    // updating just one property
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        Trace.TraceInformation($"[Controller]: Property changed: {name}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        field = value;
        OnPropertyChanged(name);
    }
}
