// Author : Murali

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace FileSynchroniser.ViewModels;

/// <summary>
/// Builds a dispatcher that marshals work onto the current UI thread.
/// </summary>
internal static class UiDispatcher
{
    /// <summary>
    /// Returns an action that runs work on the current UI thread, or null when no
    /// WinUI runtime/dispatcher exists (e.g. unit tests), in which case callers run inline.
    /// </summary>
    public static Action<Action>? Create()
    {
        try
        {
            DispatcherQueue? queue = DispatcherQueue.GetForCurrentThread();

            if (queue is null)
            {
                return null;
            }

            return action => {
                if (queue.HasThreadAccess)
                {
                    action();
                }
                else
                {
                    queue.TryEnqueue(() => action());
                }
            };
        }
        catch (COMException)
        {
            // "Class not registered": the Windows App SDK runtime isn't available.
            return null;
        }
    }
}
