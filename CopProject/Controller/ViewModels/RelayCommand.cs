// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System.Diagnostics;
using System.Windows.Input;

namespace Controller.ViewModels;

// A button action: new RelayCommand(Save). Bind a Button's Command to it.
public sealed class RelayCommand : ICommand
{
    private readonly Action _action;

    public RelayCommand(Action action) { _action = action; }

    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        Trace.TraceInformation("[Controller]: Executing command.");
        _action();
    }
}
