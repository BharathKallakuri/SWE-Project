// Author = Vishesh Srivastava
// Roll Number = 112301037

namespace LocationTracker.Models;

public record LocationSearchResult(
    double Latitude,
    double Longitude,
    string? Address
);
