using System.Diagnostics;

namespace Utils.Diagnostics;

/// <summary>
/// Writes Trace messages from this process to a file for troubleshooting.
/// </summary>
public static class FileTraceListener
{
    /// <summary>
    /// Initializes the file trace listener. 
    /// </summary>
    public static void Initialize()
    {
        /* The log file created for the current app run */
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            $"PawPatrol-{Guid.NewGuid():N}.txt");

        Trace.Listeners.Add(new TextWriterTraceListener(logPath));
        Trace.AutoFlush = true;

        Trace.TraceInformation("application started.");
        Trace.TraceWarning("some warning.");
        Trace.TraceError("some error.");

    }
}
