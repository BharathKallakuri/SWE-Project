
using System;
using System.Windows.Input;

namespace Incidents.ViewModels.Utils;

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute
            ?? throw new ArgumentNullException(nameof(execute));
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }
}
