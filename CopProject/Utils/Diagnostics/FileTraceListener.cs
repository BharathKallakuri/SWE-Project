
using System.Diagnostics;

namespace Utils.Diagnostics;

/// <summary>
/// Writes Trace messages from this process to a file for troubleshooting.
/// </summary>
public static class FileTraceListener
{
    private static string? _logPath;

    /// <summary>
    /// Initializes the file trace listener.
    /// </summary>
    public static void Initialize()
    {
        /* The log file is created in C:\Users\<username>\AppData\Local\PawPatrol */
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PawPatrol");

        Directory.CreateDirectory(logDirectory);

        _logPath = Path.Combine(
            logDirectory,
            $"PawPatrol-{Guid.NewGuid():N}.txt");

        Trace.Listeners.Add(new TextWriterTraceListener(_logPath));
        Trace.AutoFlush = true;

        Trace.TraceInformation(
            $"Application started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

}
