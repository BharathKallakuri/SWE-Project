using Microsoft.UI.Xaml.Controls;
using Incident_module.ViewModels;

namespace Incident_module.Views;

public sealed partial class AddEvidenceDialog : ContentDialog
{
    public AddEvidenceDialog()
    {
        InitializeComponent();

        DataContext = new AddEvidenceDialogViewModel();
    }
}
