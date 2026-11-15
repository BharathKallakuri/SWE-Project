// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System;
using Controller.ViewModels;
using Xunit;

namespace Controller.Test;

public class RelayCommandTests
{
    [Fact]
    public void CanExecute_AlwaysReturnsTrue()
    {
        var cmd = new RelayCommand(() => { });

        Assert.True(cmd.CanExecute(null));
        Assert.True(cmd.CanExecute("test"));
        Assert.True(cmd.CanExecute(123));
    }

    [Fact]
    public void Execute_InvokesAction()
    {
        bool executed = false;
        var cmd = new RelayCommand(() => executed = true);

        cmd.Execute(null);

        Assert.True(executed);
    }

    [Fact]
    public void CanExecuteChanged_AddRemove_DoesNotThrow()
    {
        var cmd = new RelayCommand(() => { });
        EventHandler handler = (s, e) => { };

        cmd.CanExecuteChanged += handler;
        cmd.CanExecuteChanged -= handler;
    }
}

