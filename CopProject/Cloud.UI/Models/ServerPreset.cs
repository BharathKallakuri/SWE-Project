// Author = Simeon Kandan Sonar
// Roll Number = 112301031

namespace Cloud.UI.Models;

/// <summary>
/// A hardcoded server the user can pick from the dropdown.
/// </summary>
/// <param name="Name">the text shown in the dropdown</param>
/// <param name="Url">the hub URL put into the Server URL box</param>
public sealed record ServerPreset(string Name, string Url);
