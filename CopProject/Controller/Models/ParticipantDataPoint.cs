// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System.Diagnostics;

namespace Controller.Models;
//class for maintaining participantdatapoint
public sealed class ParticipantDataPoint
{
    public double Seconds { get; set; }
    public string TimeLabel { get; set; } = "00:00";
    public int Count { get; set; }
}
