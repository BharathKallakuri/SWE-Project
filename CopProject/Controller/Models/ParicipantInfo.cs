// Author = Balabadra Lepakshi Kalyan
// Roll Number = 112301003
using System;
using System.Diagnostics;
using System.IO;
using Controller.ViewModels;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace Controller.Models;
// Represents a meeting participant and stores their details, role, and profile picture.
public sealed class ParticipantInfo : ObservableObject
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public byte[]? ProfilePicture { get; set; }

    public string RoleDisplay => IsHost ? "Host" : "Client";
    public string DisplayName => IsHost ? $"{Username} (Host)" : Username;

    // Converts the stored profile picture bytes into an ImageSource for display in the UI.
    public ImageSource? AvatarImage
    {
        get
        {
            if (ProfilePicture == null || ProfilePicture.Length == 0) return null;
            try
            {
                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(ProfilePicture);
                bitmap.SetSource(ms.AsRandomAccessStream());
                return bitmap;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[Controller]: Failed to decode avatar image for user '{Username}': {ex.Message}");
                return null;
            }
        }
    }
}
