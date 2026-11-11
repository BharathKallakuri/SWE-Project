namespace SecureMeet.Shared.Models;
public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool HasProfilePicture { get; set; }
}
