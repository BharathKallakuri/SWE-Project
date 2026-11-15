// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Controller.ViewModels;

// A button action: new RelayCommand(Save). Bind a Button's Command to it.
public sealed class RelayCommand : ICommand
{
    readonly Action action;

    public RelayCommand(Action action) { this.action = action; }

    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        Trace.TraceInformation("[RelayCommand] Executing command.");
        action();
    }
}
