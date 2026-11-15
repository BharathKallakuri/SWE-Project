// Author = Ankit Gupta
// Roll Number = 112301010

namespace LocationTracker.Models;

public static class Coordinates
{
    /// <summary>
    /// Checks if the given latitude and longitude are valid coordinates.
    /// </summary>
    public static bool IsValid(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }
}

