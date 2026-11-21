// Author: M Sreenath Karthick
// Roll Number: 112301042

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;

namespace Incidents.Views;

/// <summary>
/// Dialog where the user adds a file and description as incident evidence.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class AddEvidenceDialog : ContentDialog
{
    /// <summary>
    /// Creates the dialog and loads its XAML layout.
    /// </summary>
    public AddEvidenceDialog()
    {
        InitializeComponent();
    }
}
