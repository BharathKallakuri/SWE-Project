// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System;

namespace Controller.Models;

//class for security audit event logging
public sealed class SecurityAuditEvent
{
    public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string User { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string Status { get; set; } = "Success";
}