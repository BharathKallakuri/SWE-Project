using LocationTracker.Models;

namespace Location.Test;

/// <summary>
/// Tests for the LocationSearchResult record.
/// </summary>
[TestClass]
public class LocationSearchResultTests
{
    /// <summary>
    /// The constructor should keep all the values it is given.
    /// </summary>
    [TestMethod]
    public void ConstructorValidValues()
    {
        LocationSearchResult result = new(10.8069, 76.7254, "IIT Palakkad");

        Assert.AreEqual(10.8069, result.Latitude);
        Assert.AreEqual(76.7254, result.Longitude);
        Assert.AreEqual("IIT Palakkad", result.Address);
    }

    /// <summary>
    /// The address can be null.
    /// </summary>
    [TestMethod]
    public void ConstructorNullAddress()
    {
        LocationSearchResult result = new(1, 2, null);

        Assert.IsNull(result.Address);
    }

    /// <summary>
    /// Records with the same values should be equal.
    /// </summary>
    [TestMethod]
    public void EqualsSameValues()
    {
        LocationSearchResult first = new(1.5, 2.5, "Somewhere");
        LocationSearchResult second = new(1.5, 2.5, "Somewhere");

        Assert.AreEqual(first, second);
    }
}
