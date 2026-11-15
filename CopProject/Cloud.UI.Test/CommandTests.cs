// Author = Simeon Kandan Sonar
// Roll Number = 112301031 

using Cloud.UI.Commands;
using Cloud.UI.ViewModels;

namespace Cloud.UI.Test;

[TestClass]
public class AsyncCommandBaseTests
{
    private sealed class TestCommand : AsyncCommandBase
    {
        public bool? Allowed { get; set; }
        public Func<Task> Body { get; set; } = () => Task.CompletedTask;
        public int Runs { get; private set; }

        protected override bool CanRun()
        {
            return Allowed ?? base.CanRun(); // null: use the default (true)
        }

        protected override async Task OnExecuteAsync()
        {
            Runs++;
            await Body();
        }
    }

    [TestMethod]
    public void CanExecuteDefaultsToTrue()
    {
        Assert.IsTrue(new TestCommand().CanExecute(null));
    }

    [TestMethod]
    public async Task ExecuteAsyncWhenNotAllowedDoesNothing()
    {
        TestCommand command = new() { Allowed = false };

        await command.ExecuteAsync();

        Assert.IsFalse(command.CanExecute(null));
        Assert.AreEqual(0, command.Runs);
    }

    [TestMethod]
    public async Task WhileRunningCannotExecuteAndSecondCallIsIgnored()
    {
        TaskCompletionSource gate = new();
        TestCommand command = new() { Body = () => gate.Task };

        Task first = command.ExecuteAsync();
        bool canExecuteWhileRunning = command.CanExecute(null);
        await command.ExecuteAsync(); // ignored

        Assert.IsFalse(canExecuteWhileRunning);
        Assert.AreEqual(1, command.Runs);

        gate.SetResult();
        await first;
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public async Task ExecuteAsyncRunsOnceAndRaisesCanExecuteChangedAtStartAndEnd()
    {
        TestCommand command = new();
        int raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        await command.ExecuteAsync();

        Assert.AreEqual(2, raised);
        Assert.AreEqual(1, command.Runs);
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public async Task ExecuteAsyncWhenBodyThrowsPropagatesAndResets()
    {
        TestCommand command = new() { Body = () => throw new InvalidOperationException("boom") };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(command.ExecuteAsync);

        Assert.IsTrue(command.CanExecute(null));
    }

}

[TestClass]
public class NotificationCommandTests
{
    [TestMethod]
    public async Task CommandFailureIsShownAsErrorNotThrown()
    {
        NotificationServiceViewModel vm = new(url => TestMessenger.Create(sendError: new InvalidOperationException("send failed")).Object, ViewModelHarness.RunNow) {
            ServerUrl = ViewModelHarness.Url
        };
        await vm.ConnectCommand.ExecuteAsync();
        vm.MessageText = "hi";

        await vm.SendCommand.ExecuteAsync();

        Assert.AreEqual("send failed", vm.ErrorText);
        Assert.AreEqual("hi", vm.MessageText, "the text is kept so the user can retry");
        Assert.AreEqual(ConnectionState.Connected, vm.State);
        Assert.IsTrue(vm.SendCommand.CanExecute(null), "busy flag must be released after a failure");
    }

    [TestMethod]
    public void ExecuteICommandEntryPointRunsTheCommand()
    {
        ViewModelHarness h = new();
        h.ViewModel.ServerUrl = ViewModelHarness.Url;

        h.ViewModel.ConnectCommand.Execute(null);

        Assert.AreEqual(1, h.Messengers.Count);
        Assert.AreEqual(ConnectionState.Connected, h.ViewModel.State);
    }

}
