// Author : Madhav P Nair
// Roll Number : 112301020

using System.Diagnostics;

namespace Utils.Diagnostics;

/// <summary>
/// Writes Trace messages from this process to a file for troubleshooting.
/// </summary>
public static class FileTraceListener
{
    private static string? s_logPath;
    private static string? s_logDirectory;
    private static string? s_logPathForReading;
    private static TextWriterTraceListener? s_listener;

    /// <summary>
    /// Initializes the file trace listener.
    /// </summary>
    public static void Initialize()
    {
        /* The log file is created in C:\Users\<username>\AppData\Local\PawPatrol */
        s_logDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "PawPatrol");

        Directory.CreateDirectory(s_logDirectory);

        s_logPath = Path.Combine(
            s_logDirectory,
            $"PawPatrol-{Guid.NewGuid():N}.txt");


        var fileStream = new FileStream(
            s_logPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite); /* permits simulaneous reading and writing of the log file.*/

        s_listener = new TextWriterTraceListener(fileStream);

        Trace.Listeners.Add(s_listener);
        Trace.AutoFlush = true;

        Trace.TraceInformation(
            $"Application started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    /// <summary>
    /// Reads the most recent application log entries.
    /// </summary>
    public static async Task<string> ReadRecentLogAsync(int lineCount = 250)
    {
        if (s_logPath is null || s_logDirectory is null)
        {
            Trace.TraceError(
                "FileTraceListener has not been initialized.");
            throw new InvalidOperationException(
                "FileTraceListener has not been initialized.");
        }

        Trace.Flush(); /* Ensure all log entries are written to the file from the buffer before reading.*/

        s_logPathForReading = Path.Combine(
            s_logDirectory,
            $"{Path.GetFileNameWithoutExtension(s_logPath)}-read.txt");

        /* Create a separate snapshot of the original log.*/
        File.Copy(s_logPath, s_logPathForReading, true);

        /* Read the snapshot, not the original log file.*/
        string[] lines = await File.ReadAllLinesAsync(s_logPathForReading);

        IEnumerable<string> recentLines = lines.TakeLast(lineCount);

        Trace.TraceInformation(
            $"Reading the last {lineCount} lines from the log snapshot.");

        return string.Join(Environment.NewLine, recentLines);
    }
}
