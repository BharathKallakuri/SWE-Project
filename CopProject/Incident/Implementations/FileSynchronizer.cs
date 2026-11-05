using System;
using System.IO;
using System.Security;

namespace Incident_module.Implementations;

using FileSynchroniser.Models;

/// <summary>
/// Provides mock implementation of file synchronisation service for incident management module.
/// </summary>
public class IncidentFileSynchroniser : IFileOperations
{
    private readonly Dictionary<string, byte[]> _memory = new();

    /// <summary>
    /// Returns the mock path to the synchronised root directory.
    /// </summary>
    public string GetDirectory()
    {
        return Path.GetFullPath(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
        );
    }

    /// <summary>
    /// Saves a new file in the mocksynchronised root directory.
    /// </summary>
    public bool SaveFile(string relativePath, byte[] content)
    {
        _memory[relativePath] = content;
        return true;
    }

    /// <summary>
    /// Reads a file from the mock synchronised root directory
    /// </summary>
    public byte[] ReadFile(string relativePath)
    {
        return _memory.TryGetValue(relativePath, out byte[]? content) ? content : [];
    }

    /// <summary>
    /// Updates an existing file in the mock synchronised root directory.
    /// </summary>
    public bool UpdateFile(string relativePath, byte[] content)
    {
        if (!_memory.ContainsKey(relativePath))
        {
            throw new FileNotFoundException("The file does not exist: ", relativePath);
        }

        _memory[relativePath] = content;
        return true;
    }

    /// <summary>
    /// Deletes a file from the mock synchronised root directory.
    /// </summary>
    public bool DeleteFile(string relativePath)
    {
        if (!_memory.ContainsKey(relativePath))
        {
            throw new FileNotFoundException("The file does not exist: ", relativePath);
        }

        _memory.Remove(relativePath);
        return true;
    }

    /// <summary>
    /// Checks whether a file exists in the mock synchronised root directory.
    /// </summary>
    public bool FileExists(string relativePath)
    {
        return _memory.ContainsKey(relativePath);
    }
}
