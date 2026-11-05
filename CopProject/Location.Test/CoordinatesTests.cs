using LocationTracker.Models;

namespace Location.Test;

/// <summary>
/// Tests for the Coordinates validator.
/// </summary>
[TestClass]
public class CoordinatesTests
{
    /// <summary>
    /// Normal coordinates should be valid.
    /// </summary>
    [TestMethod]
    public void NormalCoordinated()
    {
        Assert.IsTrue(Coordinates.IsValid(10.8069, 76.7254));
        Assert.IsTrue(Coordinates.IsValid(0, 0));
    }

    /// <summary>
    /// The extreme ends of the range should still be valid.
    /// </summary>
    [TestMethod]
    public void BoundaryValues()
    {
        Assert.IsTrue(Coordinates.IsValid(90, 180));
        Assert.IsTrue(Coordinates.IsValid(-90, -180));
    }

    /// <summary>
    /// Latitude outside -90..90 should be invalid.
    /// </summary>
    [TestMethod]
    public void LatituteOutOfRange()
    {
        Assert.IsFalse(Coordinates.IsValid(91, 0));
        Assert.IsFalse(Coordinates.IsValid(-91, 0));
    }

    /// <summary>
    /// Longitude outside -180..180 should be invalid.
    /// </summary>
    [TestMethod]
    public void LongitudeOutOfRange()
    {
        Assert.IsFalse(Coordinates.IsValid(0, 181));
        Assert.IsFalse(Coordinates.IsValid(0, -181));
    }

    /// <summary>
    /// NaN and infinity should be invalid.
    /// </summary>
    [TestMethod]
    public void NanOrInfinityAsInput()
    {
        Assert.IsFalse(Coordinates.IsValid(double.NaN, 0));
        Assert.IsFalse(Coordinates.IsValid(0, double.PositiveInfinity));
    }
}
