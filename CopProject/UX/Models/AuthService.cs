using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using SecureMeet.Shared.Models;

namespace UX.Models;
public sealed class AuthService
{
    public const string AllowedDomain = "@smail.iitpkd.ac.in";
    private readonly string _file;
    private readonly List<UserProfile> _users = new();
    public AuthService(string folder)
    {
        _file = Path.Combine(folder, "users.json");
        try
        {
            if (File.Exists(_file))
            {
                _users = JsonSerializer.Deserialize<List<UserProfile>>(File.ReadAllText(_file)) ?? new();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("AuthService: could not read " + _file + ": " + ex.Message);
        }
    }
    public IReadOnlyList<UserProfile> Users => _users.ToList();
    public UserProfile? LastUser => _users.LastOrDefault();
    public UserProfile? Find(string email)
    {
        email = NormalizeEmail(email);
        return _users.FirstOrDefault(u => NormalizeEmail(u.Email) == email);
    }
    public UserProfile? SignIn(string email, string userId, out string error)
    {
        error = string.Empty;
        email = NormalizeEmail(email);

        bool taken = _users.Any(u => NormalizeEmail(u.Email) != email
            && string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (taken)
        {
            error = "This User ID is already used by someone else.";
            return null;
        }

        UserProfile? old = Find(email);
        var user = new UserProfile { UserId = userId, Email = email, HasProfilePicture = old?.HasProfilePicture ?? false };
        if (old != null)
        {
            _users.Remove(old);
        }

        _users.Add(user);
        Save();
        return user;
    }
    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
    public static bool IsValidEmail(string email)
    {
        if (!email.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            return new MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }
    public static string UserIdProblem(string userId)
    {
        if (userId.Length < 2)
        {
            return "User ID must be at least 2 characters.";
        }

        if (userId.Length > 30)
        {
            return "User ID can be at most 30 characters.";
        }

        if (!Regex.IsMatch(userId, "^[A-Za-z0-9._-]+$"))
        {
            return "User ID can only have letters, numbers, dot, dash and underscore.";
        }

        return string.Empty;
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            File.WriteAllText(_file, JsonSerializer.Serialize(_users));
        }
        catch (Exception ex)
        {
            Debug.WriteLine("AuthService: could not save " + _file + ": " + ex.Message);
        }
    }
}
