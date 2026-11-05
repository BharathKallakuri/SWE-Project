
using System;
using Incident_module;
using Incident_module.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

/// <summary>
/// Testing AddEvidenceDialogViewModel functionality.
/// </summary>
[TestClass]
public sealed class AddEvidenceDialogViewModelTest
{
    private AddEvidenceDialogViewModel _viewModel = null!;

    /// <summary>
    /// Initializing AddEvidenceDialogViewModel for testing.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _viewModel = new AddEvidenceDialogViewModel(IntPtr.Zero);
    }

    /// <summary>
    /// 1. Default property values are initialized correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesDefaultValues()
    {
        Assert.AreEqual(string.Empty, _viewModel.FilePath);
        Assert.AreEqual(string.Empty, _viewModel.Description);
        Assert.IsFalse(_viewModel.IsValid);
    }

    /// <summary>
    /// 2. Setting FilePath updates its value.
    /// </summary>
    [TestMethod]
    public void FilePath_SetValue_UpdatesProperty()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";

        Assert.AreEqual("C:/Evidence/image.png", _viewModel.FilePath);
    }

    /// <summary>
    /// 3. Setting Description updates its value.
    /// </summary>
    [TestMethod]
    public void Description_SetValue_UpdatesProperty()
    {
        _viewModel.Description = "Evidence description";

        Assert.AreEqual("Evidence description", _viewModel.Description);
    }

    /// <summary>
    /// 4. IsValid returns true when both fields are populated.
    /// </summary>
    [TestMethod]
    public void IsValid_BothFieldsPopulated_ReturnsTrue()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";
        _viewModel.Description = "Evidence description";

        Assert.IsTrue(_viewModel.IsValid);
    }

    /// <summary>
    /// 5. IsValid returns false when FilePath is empty.
    /// </summary>
    [TestMethod]
    public void IsValid_EmptyFilePath_ReturnsFalse()
    {
        _viewModel.Description = "Evidence description";

        Assert.IsFalse(_viewModel.IsValid);
    }

    /// <summary>
    /// 6. IsValid returns false when Description is empty.
    /// </summary>
    [TestMethod]
    public void IsValid_EmptyDescription_ReturnsFalse()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";

        Assert.IsFalse(_viewModel.IsValid);
    }

    /// <summary>
    /// 7. FilePath setter raises PropertyChanged notifications.
    /// </summary>
    [TestMethod]
    public void FilePath_SetValue_RaisesPropertyChangedEvents()
    {
        int eventCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.FilePath) ||
                e.PropertyName == nameof(_viewModel.IsValid))
            {
                eventCount++;
            }
        };

        _viewModel.FilePath = "C:/Evidence/image.png";

        Assert.AreEqual(2, eventCount);
    }

    /// <summary>
    /// 8. Description setter raises PropertyChanged notifications.
    /// </summary>
    [TestMethod]
    public void Description_SetValue_RaisesPropertyChangedEvents()
    {
        int eventCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.Description) ||
                e.PropertyName == nameof(_viewModel.IsValid))
            {
                eventCount++;
            }
        };

        _viewModel.Description = "Evidence description";

        Assert.AreEqual(2, eventCount);
    }

    /// <summary>
    /// 9. BuildEvidence creates an evidence object with the supplied details.
    /// </summary>
    [TestMethod]
    public void BuildEvidence_CreatesEvidenceWithCorrectValues()
    {
        Guid uploadedBy = Guid.NewGuid();

        _viewModel.FilePath = "C:/Evidence/image.png";
        _viewModel.Description = "  Accident photograph  ";

        IncidentEvidence evidence = _viewModel.BuildEvidence(uploadedBy);

        Assert.AreNotEqual(Guid.Empty, evidence.Id);
        Assert.AreEqual("Accident photograph", evidence.Description);
        Assert.AreEqual("C:/Evidence/image.png", evidence.FilePath);
        Assert.AreEqual(uploadedBy, evidence.UploadedBy);
        Assert.AreNotEqual(default, evidence.UploadTime);
    }

    /// <summary>
    /// 10. BuildEvidence generates a unique ID for each evidence object.
    /// </summary>
    [TestMethod]
    public void BuildEvidence_GeneratesUniqueIds()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";
        _viewModel.Description = "Evidence";

        IncidentEvidence first = _viewModel.BuildEvidence(Guid.NewGuid());
        IncidentEvidence second = _viewModel.BuildEvidence(Guid.NewGuid());

        Assert.AreNotEqual(first.Id, second.Id);
    }

    /// <summary>
    /// 11. Setting FilePath to the same value does not raise PropertyChanged events.
    /// </summary>
    [TestMethod]
    public void FilePath_SetSameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.FilePath = "C:/Evidence/image.png";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.FilePath = "C:/Evidence/image.png",
            nameof(_viewModel.FilePath));
    }

    /// <summary>
    /// 12. Setting Description to the same value does not raise PropertyChanged events.
    /// </summary>
    [TestMethod]
    public void Description_SetSameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.Description = "Evidence description";

        VMTestHelpers.AssertNoPropertyChanged(
            _viewModel,
            () => _viewModel.Description = "Evidence description",
            nameof(_viewModel.Description));
    }
}
