
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Incident_module;
using Incident_module.Implementations;
using Incident_module.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Incidents.Test.ViewModels;

[TestClass]
public class AllIncidentsPageViewModelTests
{
    private IIncidentController _controller = null!;
    private AllIncidentsPageViewModel _viewModel = null!;

    private readonly Guid _user1 =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Guid _user2 =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Incident _vehicleIncident = null!;
    private Incident _theftIncident = null!;
    private Incident _drunkDrivingIncident = null!;
    private Incident _crashIncident = null!;


    [TestInitialize]
    public async Task Setup()
    {
        // Create actual storage and file synchronisation implementations.
        IncidentCloudStorageService storageService = new();
        IncidentFileSynchroniser fileSynchroniser = new();

        // Initialize the real IncidentController.
        _controller = await IncidentController.CreateAsync(
            storageService,
            fileSynchroniser);

        // Create incidents through the controller.
        _vehicleIncident = await CreateTestIncident(
            "Vehicle Accident",
            _user1,
            IncidentCategory.Accident,
            IncidentSeverity.High);

        _theftIncident = await CreateTestIncident(
            "Stolen Bicycle",
            _user2,
            IncidentCategory.Theft,
            IncidentSeverity.Medium);

        _drunkDrivingIncident = await CreateTestIncident(
            "Drunk Driving Near School",
            _user1,
            IncidentCategory.DrunkDriving,
            IncidentSeverity.Critical);

        _crashIncident = await CreateTestIncident(
            "Crash [North Road]",
            _user2,
            IncidentCategory.Accident,
            IncidentSeverity.Low);

        // Set different statuses using the real controller.
        await _controller.UpdateStatus(
            _theftIncident,
            IncidentStatus.InProgress);

        await _controller.UpdateStatus(
            _drunkDrivingIncident,
            IncidentStatus.Resolved);

        // Initialize the ViewModel with the real controller.
        _viewModel = new AllIncidentsPageViewModel(
            _controller,
            _user1);

        // Load incidents into the ViewModel.
        await _viewModel.LoadAsync();
    }

    private async Task<Incident> CreateTestIncident(
        string title,
        Guid createdBy,
        IncidentCategory category,
        IncidentSeverity severity)
    {
        CreateIncidentRequest request = new(
            title,
            "Test incident description",
            createdBy,
            category,
            severity,
            new IncidentLocation(10.0, 76.0, "Test location"),
            []);

        return await _controller.CreateIncident(request);
    }

    // Constructor Creation tests

    /// <summary>
    /// Test 1: Constructor initializes all filter selections to their defaults.
    /// </summary>
    [TestMethod]
    public void Constructor_DefaultValues_InitializesCorrectly()
    {
        AllIncidentsPageViewModel viewModel =
            new(_controller, _user1);

        Assert.AreEqual(string.Empty, viewModel.SearchText);

        Assert.AreEqual("All", viewModel.SelectedStatus);
        Assert.AreEqual("All", viewModel.SelectedCategory);
        Assert.AreEqual("All", viewModel.SelectedSeverity);

        Assert.IsNull(viewModel.FromDate);
        Assert.IsNull(viewModel.ToDate);

        Assert.AreEqual(string.Empty, viewModel.CreatedByInput);

        Assert.IsEmpty(viewModel.Incidents);
    }

    /// <summary>
    /// Test 2: Constructor creates dropdown options from the enums.
    /// </summary>
    [TestMethod]
    public void Constructor_CreatesDropdownOptions()
    {
        CollectionAssert.AreEqual(
            new List<string>
            {
                "All",
                "Reported",
                "InProgress",
                "Resolved"
            },
            _viewModel.StatusOptions);

        CollectionAssert.AreEqual(
            new List<string>
            {
                "All",
                "DrunkDriving",
                "Theft",
                "Accident"
            },
            _viewModel.CategoryOptions);

        CollectionAssert.AreEqual(
            new List<string>
            {
                "All",
                "Low",
                "Medium",
                "High",
                "Critical"
            },
            _viewModel.SeverityOptions);
    }

    /// <summary>
    /// Test 3: Constructor throws an exception when the controller is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullController_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new AllIncidentsPageViewModel(null!, _user1));
    }

    /// <summary>
    /// Test 4: LoadAsync retrieves incidents from the controller.
    /// </summary>
    [TestMethod]
    public async Task LoadAsync_LoadsIncidentsFromController()
    {
        await _viewModel.LoadAsync();

        Assert.HasCount(4, _viewModel.Incidents);

        Assert.IsTrue(
            _viewModel.Incidents.Any(
                i => i.Title == "Vehicle Accident"));

        Assert.IsTrue(
            _viewModel.Incidents.Any(
                i => i.Title == "Stolen Bicycle"));
    }

    /// <summary>
    /// Test 5: LoadAsync sorts incidents by UpdateTime in descending order.
    /// </summary>
    [TestMethod]
    public async Task LoadAsync_SortsIncidentsByUpdateTimeDescending()
    {
        await _viewModel.LoadAsync();

        for (int i = 0; i < _viewModel.Incidents.Count - 1; i++)
        {
            Assert.IsTrue(
                _viewModel.Incidents[i].UpdateTime >=
                _viewModel.Incidents[i + 1].UpdateTime);
        }
    }

    // ---------------------------------------------------------
    // Fielter tests
    // ---------------------------------------------------------

    /// <summary>
    /// Test 6: SearchText filters incidents by their title.
    /// </summary>
    [TestMethod]
    public void SearchText_MatchingTitle_ReturnsMatchingIncidents()
    {
        _viewModel.SearchText = "Vehicle";

        Assert.HasCount(1, _viewModel.Incidents);
        Assert.AreEqual(
            "Vehicle Accident",
            _viewModel.Incidents[0].Title);
    }

    /// <summary>
    /// Test 7: SearchText trims leading and trailing whitespace.
    /// </summary>
    [TestMethod]
    public void SearchText_ContainsWhitespace_TrimsSearchText()
    {
        _viewModel.SearchText = "  Vehicle  ";

        Assert.HasCount(1, _viewModel.Incidents);
        Assert.AreEqual(
            "Vehicle Accident",
            _viewModel.Incidents[0].Title);
    }

    /// <summary>
    /// Test 8: SearchText treats regex special characters literally.
    /// </summary>
    [TestMethod]
    public void SearchText_RegexSpecialCharacters_SearchesLiterally()
    {
        _viewModel.SearchText = "[";

        Assert.HasCount(1, _viewModel.Incidents);
        Assert.AreEqual(
            "Crash [North Road]",
            _viewModel.Incidents[0].Title);
    }

    /// <summary>
    /// Test 9: SearchText returns no incidents when there are no matches.
    /// </summary>
    [TestMethod]
    public void SearchText_NoMatchingTitle_ReturnsEmptyCollection()
    {
        _viewModel.SearchText = "Nonexistent";

        Assert.IsEmpty(_viewModel.Incidents);
    }

    /// <summary>
    /// Test 10: SelectedStatus filters incidents by status.
    /// </summary>
    [TestMethod]
    public void SelectedStatus_FiltersByStatus()
    {
        _viewModel.SelectedStatus =
            nameof(IncidentStatus.Reported);

        Assert.HasCount(2, _viewModel.Incidents);

        Assert.IsTrue(
            _viewModel.Incidents.All(
                i => i.Status == IncidentStatus.Reported));
    }

    /// <summary>
    /// Test 11: SelectedCategory filters incidents by category.
    /// </summary>
    [TestMethod]
    public void SelectedCategory_FiltersByCategory()
    {
        _viewModel.SelectedCategory =
            nameof(IncidentCategory.Accident);

        Assert.HasCount(2, _viewModel.Incidents);

        Assert.IsTrue(
            _viewModel.Incidents.All(
                i => i.Category == IncidentCategory.Accident));
    }

    /// <summary>
    /// Test 12: SelectedSeverity filters incidents by severity.
    /// </summary>
    [TestMethod]
    public void SelectedSeverity_FiltersBySeverity()
    {
        _viewModel.SelectedSeverity =
            nameof(IncidentSeverity.High);

        Assert.HasCount(1, _viewModel.Incidents);

        Assert.AreEqual(
            IncidentSeverity.High,
            _viewModel.Incidents[0].Severity);
    }


    /// <summary>
    /// Test 13: FromDate includes incidents created after the selected date.
    /// </summary>
    [TestMethod]
    public void FromDate_FiltersIncidentsFromSelectedDate()
    {
        _viewModel.FromDate =
            new DateTimeOffset(DateTime.Now.AddDays(-1));

        Assert.HasCount(4, _viewModel.Incidents);
    }

    /// <summary>
    /// Test 14: FromDate excludes incidents created before the selected date.
    /// </summary>
    [TestMethod]
    public void FromDate_FutureDate_ReturnsNoIncidents()
    {
        _viewModel.FromDate =
            new DateTimeOffset(DateTime.Now.AddDays(1));

        Assert.HasCount(0, _viewModel.Incidents);
    }

    /// <summary>
    /// Test 15: ToDate includes incidents created before the selected date.
    /// </summary>
    [TestMethod]
    public void ToDate_FutureDate_ReturnsAllIncidents()
    {
        _viewModel.ToDate =
            new DateTimeOffset(DateTime.Now.AddDays(1));

        Assert.HasCount(4, _viewModel.Incidents);
    }

    /// <summary>
    /// Test 16: ToDate excludes incidents created after the selected date.
    /// </summary>
    [TestMethod]
    public void ToDate_PastDate_ReturnsNoIncidents()
    {
        _viewModel.ToDate =
            new DateTimeOffset(DateTime.Now.AddDays(-1));

        Assert.HasCount(0, _viewModel.Incidents);
    }

    /// <summary>
    /// Test 17: FromDate and ToDate together filter a date range.
    /// </summary>
    [TestMethod]
    public void FromDateAndToDate_DateRange_ReturnsMatchingIncidents()
    {
        _viewModel.FromDate =
            new DateTimeOffset(DateTime.Now.AddDays(-1));

        _viewModel.ToDate =
            new DateTimeOffset(DateTime.Now.AddDays(1));

        Assert.HasCount(4, _viewModel.Incidents);
    }

    /// <summary>
    /// Test 18: CreatedByInput filters incidents using a valid GUID.
    /// </summary>
    [TestMethod]
    public void CreatedByInput_ValidGuid_FiltersByCreator()
    {
        _viewModel.CreatedByInput = _user1.ToString();

        Assert.HasCount(2, _viewModel.Incidents);

        Assert.IsTrue(
            _viewModel.Incidents.All(
                i => i.CreatedBy == _user1));
    }

    /// <summary>
    /// Test 19: CreatedByInput trims whitespace around a valid GUID.
    /// </summary>
    [TestMethod]
    public void CreatedByInput_ValidGuidWithWhitespace_FiltersByCreator()
    {
        _viewModel.CreatedByInput = $"  {_user1}  ";

        Assert.HasCount(2, _viewModel.Incidents);
    }

    /// <summary>
    /// Test 20: Invalid GUID input does not apply a creator filter.
    /// </summary>
    [TestMethod]
    public void CreatedByInput_InvalidGuid_DoesNotFilterIncidents()
    {
        _viewModel.CreatedByInput = "invalid-guid";

        Assert.HasCount(4, _viewModel.Incidents);
    }


    /// <summary>
    /// Test 21: Multiple filters are applied together.
    /// </summary>
    [TestMethod]
    public void MultipleFilters_AppliedTogether_ReturnsMatchingIncidents()
    {
        _viewModel.SearchText = "Driving";

        _viewModel.SelectedStatus =
            nameof(IncidentStatus.Resolved);

        _viewModel.SelectedCategory =
            nameof(IncidentCategory.DrunkDriving);

        _viewModel.SelectedSeverity =
            nameof(IncidentSeverity.Critical);

        _viewModel.CreatedByInput = _user1.ToString();

        Assert.HasCount(1, _viewModel.Incidents);

        Assert.AreEqual(
            "Drunk Driving Near School",
            _viewModel.Incidents[0].Title);
    }

    // ClearFilters tests

    /// <summary>
    /// Test 22: ClearFilters resets all filter properties.
    /// </summary>
    [TestMethod]
    public void ClearFilters_ResetsAllFilters()
    {
        _viewModel.SearchText = "Vehicle";

        _viewModel.SelectedStatus =
            nameof(IncidentStatus.Resolved);

        _viewModel.SelectedCategory =
            nameof(IncidentCategory.Theft);

        _viewModel.SelectedSeverity =
            nameof(IncidentSeverity.High);

        _viewModel.FromDate =
            new DateTimeOffset(DateTime.Now.AddDays(-2));

        _viewModel.ToDate =
            new DateTimeOffset(DateTime.Now.AddDays(2));

        _viewModel.CreatedByInput = _user2.ToString();

        _viewModel.ClearFilters();

        Assert.AreEqual(string.Empty, _viewModel.SearchText);

        Assert.AreEqual("All", _viewModel.SelectedStatus);
        Assert.AreEqual("All", _viewModel.SelectedCategory);
        Assert.AreEqual("All", _viewModel.SelectedSeverity);

        Assert.IsNull(_viewModel.FromDate);
        Assert.IsNull(_viewModel.ToDate);

        Assert.AreEqual(string.Empty, _viewModel.CreatedByInput);

        Assert.HasCount(4, _viewModel.Incidents);
    }

    /// <summary>
    /// Test 23: ClearFiltersCommand executes ClearFilters.
    /// </summary>
    [TestMethod]
    public void ClearFiltersCommand_Execute_ClearsFilters()
    {
        _viewModel.SearchText = "Vehicle";

        _viewModel.ClearFiltersCommand.Execute(null);

        Assert.AreEqual(string.Empty, _viewModel.SearchText);
        Assert.HasCount(4, _viewModel.Incidents);
    }

    // Empty message visibility tests

    /// <summary>
    /// Test 24: EmptyMessageVisibility is Visible when no incidents match.
    /// </summary>
    [TestMethod]
    public void EmptyMessageVisibility_NoMatchingIncidents_ReturnsVisible()
    {
        _viewModel.SearchText = "Nonexistent";

        Assert.AreEqual(
            Visibility.Visible,
            _viewModel.EmptyMessageVisibility);
    }

    /// <summary>
    /// Test 25: EmptyMessageVisibility is Collapsed when incidents exist.
    /// </summary>
    [TestMethod]
    public void EmptyMessageVisibility_IncidentsExist_ReturnsCollapsed()
    {
        Assert.AreEqual(
            Visibility.Collapsed,
            _viewModel.EmptyMessageVisibility);
    }

    // Property notification tests

    /// <summary>
    /// Test 26: Changing SearchText raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SearchText_Changed_RaisesPropertyChanged()
    {
        List<string?> changedProperties = [];

        _viewModel.PropertyChanged +=
            (_, e) => changedProperties.Add(e.PropertyName);

        _viewModel.SearchText = "Vehicle";

        CollectionAssert.Contains(
            changedProperties,
            nameof(_viewModel.SearchText));
    }

    /// <summary>
    /// Test 27: Setting SearchText to the same value raises no notification.
    /// </summary>
    [TestMethod]
    public void SearchText_SameValue_DoesNotRaisePropertyChanged()
    {
        int notificationCount = 0;

        _viewModel.PropertyChanged +=
            (_, _) => notificationCount++;

        _viewModel.SearchText = string.Empty;

        Assert.AreEqual(0, notificationCount);
    }

    /// <summary>
    /// Test 28: Changing SelectedStatus raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedStatus_Changed_RaisesPropertyChanged()
    {
        bool notified = false;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.SelectedStatus))
            {
                notified = true;
            }
        };

        _viewModel.SelectedStatus =
            nameof(IncidentStatus.Resolved);

        Assert.IsTrue(notified);
    }

    /// <summary>
    /// Test 29: Changing FromDate raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void FromDate_Changed_RaisesPropertyChanged()
    {
        bool notified = false;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.FromDate))
            {
                notified = true;
            }
        };

        _viewModel.FromDate =
            new DateTimeOffset(DateTime.Now);

        Assert.IsTrue(notified);
    }

    /// <summary>
    /// Test 30: ClearFilters raises notifications for all filter properties.
    /// </summary>
    [TestMethod]
    public void ClearFilters_RaisesPropertyChangedForAllFilters()
    {
        List<string?> changedProperties = [];

        _viewModel.PropertyChanged +=
            (_, e) => changedProperties.Add(e.PropertyName);

        _viewModel.ClearFilters();

        string[] expectedProperties =
        [
            nameof(_viewModel.SearchText),
            nameof(_viewModel.SelectedStatus),
            nameof(_viewModel.SelectedCategory),
            nameof(_viewModel.SelectedSeverity),
            nameof(_viewModel.FromDate),
            nameof(_viewModel.ToDate),
            nameof(_viewModel.CreatedByInput),
            nameof(_viewModel.EmptyMessageVisibility)
        ];

        foreach (string property in expectedProperties)
        {
            CollectionAssert.Contains(
                changedProperties,
                property);
        }
    }

    /// <summary>
    /// Test 31: Setting SelectedStatus to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedStatus_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.SelectedStatus = nameof(IncidentStatus.Resolved);

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.SelectedStatus))
            {
                notificationCount++;
            }
        };

        _viewModel.SelectedStatus = nameof(IncidentStatus.Resolved);

        Assert.AreEqual(0, notificationCount);
    }

    /// <summary>
    /// Test 32: Setting SelectedCategory to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedCategory_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.SelectedCategory = nameof(IncidentCategory.Accident);

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.SelectedCategory))
            {
                notificationCount++;
            }
        };

        _viewModel.SelectedCategory = nameof(IncidentCategory.Accident);

        Assert.AreEqual(0, notificationCount);
    }

    /// <summary>
    /// Test 33: Setting SelectedSeverity to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void SelectedSeverity_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.SelectedSeverity = nameof(IncidentSeverity.High);

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.SelectedSeverity))
            {
                notificationCount++;
            }
        };

        _viewModel.SelectedSeverity = nameof(IncidentSeverity.High);

        Assert.AreEqual(0, notificationCount);
    }

    /// <summary>
    /// Test 34: Setting FromDate to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void FromDate_SameValue_DoesNotRaisePropertyChanged()
    {
        DateTimeOffset date = DateTimeOffset.Now;

        _viewModel.FromDate = date;

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.FromDate))
            {
                notificationCount++;
            }
        };

        _viewModel.FromDate = date;

        Assert.AreEqual(0, notificationCount);
    }

    /// <summary>
    /// Test 35: Setting FromDate to null when it is already null does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void FromDate_NullValue_DoesNotRaisePropertyChanged()
    {
        Assert.IsNull(_viewModel.FromDate);

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.FromDate))
            {
                notificationCount++;
            }
        };

        _viewModel.FromDate = null;

        Assert.AreEqual(0, notificationCount);
    }

    /// <summary>
    /// Test 36: Setting FromDate to null after assigning a date raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void FromDate_ResetToNull_RaisesPropertyChanged()
    {
        _viewModel.FromDate = DateTimeOffset.Now;

        bool notified = false;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.FromDate))
            { notified = true; }
        };

        _viewModel.FromDate = null;

        Assert.IsNull(_viewModel.FromDate);
        Assert.IsTrue(notified);
    }

    /// <summary>
    /// Test 37: Setting ToDate to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void ToDate_SameValue_DoesNotRaisePropertyChanged()
    {
        DateTimeOffset date = DateTimeOffset.Now;

        _viewModel.ToDate = date;

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.ToDate))
            { notificationCount++; }
        };

        _viewModel.ToDate = date;

        Assert.AreEqual(0, notificationCount);
    }

    /// <summary>
    /// Test 38: Setting ToDate to null when it is already null does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void ToDate_NullValue_DoesNotRaisePropertyChanged()
    {
        Assert.IsNull(_viewModel.ToDate);

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.ToDate))
            { notificationCount++; }
        };

        _viewModel.ToDate = null;

        Assert.AreEqual(0, notificationCount);
    }


    /// <summary>
    /// Test 39: Setting ToDate to null after assigning a date raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void ToDate_ResetToNull_RaisesPropertyChanged()
    {
        _viewModel.ToDate = DateTimeOffset.Now;

        bool notified = false;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.ToDate))
            { notified = true; }
        };

        _viewModel.ToDate = null;

        Assert.IsNull(_viewModel.ToDate);
        Assert.IsTrue(notified);
    }

    /// <summary>
    /// Test 40: Setting CreatedByInput to its existing value does not raise PropertyChanged.
    /// </summary>
    [TestMethod]
    public void CreatedByInput_SameValue_DoesNotRaisePropertyChanged()
    {
        _viewModel.CreatedByInput = _user1.ToString();

        int notificationCount = 0;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.CreatedByInput))
            { notificationCount++; }
        };

        _viewModel.CreatedByInput = _user1.ToString();

        Assert.AreEqual(0, notificationCount);
    }


    /// <summary>
    /// Test 41: Changing CreatedByInput raises PropertyChanged.
    /// </summary>
    [TestMethod]
    public void CreatedByInput_ChangedValue_RaisesPropertyChanged()
    {
        bool notified = false;

        _viewModel.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(_viewModel.CreatedByInput))
            { notified = true; }
        };

        _viewModel.CreatedByInput = _user1.ToString();

        Assert.AreEqual(_user1.ToString(), _viewModel.CreatedByInput);
        Assert.IsTrue(notified);
    }


}
