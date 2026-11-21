/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace Whiteboard.Test.Views;

[CollectionDefinition("XamlUi")]
public sealed class XamlUiCollection : ICollectionFixture<XamlUiThreadFixture>
{
}

/// <summary>
/// Hosts a single STA thread with an initialized WinUI 3 XAML runtime
/// so unit tests can instantiate and assert on XAML controls like <see cref="Canvas"/>.
/// </summary>
public sealed class XamlUiThreadFixture : IDisposable
{
    private readonly Thread _uiThread;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueController _controller;

    public XamlUiThreadFixture()
    {
        TaskCompletionSource<(DispatcherQueue, DispatcherQueueController)> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

        _uiThread = new Thread(() => {
            try
            {
                WinRtActivationContext.EnsureActivated();
                DispatcherQueueController controller = DispatcherQueueController.CreateOnCurrentThread();
                using WindowsXamlManager _ = WindowsXamlManager.InitializeForCurrentThread();
                ready.SetResult((controller.DispatcherQueue, controller));
                controller.DispatcherQueue.RunEventLoop();
            }
            catch (Exception ex)
            {
                ready.TrySetException(ex);
            }
        }) {
            IsBackground = true,
            Name = "Whiteboard.Test.XamlUiThread"
        };

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        (_dispatcherQueue, _controller) = ready.Task.GetAwaiter().GetResult();
    }

    public Task RunAsync(Action action)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_dispatcherQueue.TryEnqueue(() => {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }))
        {
            tcs.SetException(new InvalidOperationException("Failed to enqueue work onto the XAML UI thread."));
        }

        return tcs.Task;
    }

    public void Dispose()
    {
        WinRtActivationContext.EnsureActivated();
        DispatcherQueueHandler shutdown = () => _controller.ShutdownQueue();
        _dispatcherQueue.TryEnqueue(shutdown);
        _uiThread.Join(TimeSpan.FromSeconds(5));
    }
}

[Collection("XamlUi")]
public class XamlUiThreadSmokeTests(XamlUiThreadFixture ui)
{
    [Fact]
    public async Task CanInstantiateWinUiCanvasOnStaThread()
    {
        await ui.RunAsync(() => {
            Canvas canvas = new();
            Assert.NotNull(canvas);
            Assert.Empty(canvas.Children);
        });
    }
}
