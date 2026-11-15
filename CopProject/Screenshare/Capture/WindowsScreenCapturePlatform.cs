// Author = Aryan
// Roll Number = 112301041

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ScreenShare;

internal sealed class WindowsScreenCapturePlatform : IScreenCapturePlatform
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public int GetScreenWidth()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);

        if (width <= 0)
        {
            Trace.TraceWarning(
                "[Screenshare] WindowsScreenCapturePlatform.GetScreenWidth: GetSystemMetrics returned {0}.", width);
        }

        return width;
    }

    public int GetScreenHeight()
    {
        int height = GetSystemMetrics(SM_CYSCREEN);

        if (height <= 0)
        {
            Trace.TraceWarning(
                "[Screenshare] WindowsScreenCapturePlatform.GetScreenHeight: GetSystemMetrics returned {0}.", height);
        }

        return height;
    }

    public IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext)
    {
        IntPtr previousContext = NativeSetThreadDpiAwarenessContext(dpiContext);

        if (previousContext == IntPtr.Zero)
        {
            Trace.TraceWarning(
                "[Screenshare] WindowsScreenCapturePlatform.SetThreadDpiAwarenessContext: Failed to set DPI awareness context {0}.",
                dpiContext);
        }

        return previousContext;
    }

    public void CopyFromScreen(Bitmap destination, Rectangle bounds)
    {
        try
        {
            using Graphics graphics = Graphics.FromImage(destination);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        catch (Exception exception)
        {
            Trace.TraceError(
                "[Screenshare] WindowsScreenCapturePlatform.CopyFromScreen: Copy from screen failed for bounds {0}: {1}",
                bounds,
                exception);
            throw;
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetThreadDpiAwarenessContext")]
    private static extern IntPtr NativeSetThreadDpiAwarenessContext(
        IntPtr dpiContext);
}
