using System.ComponentModel.DataAnnotations;

namespace EventEase.Models;

/// <summary>Shared validation rules for a person's name and email.</summary>
internal static class PersonRules
{
    // Letters (any language, incl. accents), spaces, apostrophes, periods and hyphens.
    public const string NamePattern = @"^[\p{L}\p{M}' .\-]+$";
    public const string NameMessage = "Name can only contain letters, spaces, apostrophes, periods and hyphens.";

    // Stricter than [EmailAddress] alone, which accepts values like "a@b".
    public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$";
    public const string EmailMessage = "Enter a valid email address, e.g. name@example.com.";
}

/// <summary>The signed-in user for the current session.</summary>
public sealed class UserProfile
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Full name must be 2–80 characters.")]
    [RegularExpression(PersonRules.NamePattern, ErrorMessage = PersonRules.NameMessage)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(254, ErrorMessage = "Email is too long.")]
    [RegularExpression(PersonRules.EmailPattern, ErrorMessage = PersonRules.EmailMessage)]
    public string Email { get; set; } = string.Empty;

    public UserProfile Normalized() => new()
    {
        FullName = FullName.Trim(),
        Email = Email.Trim().ToLowerInvariant()
    };
}

/// <summary>Input model for the event Registration Form.</summary>
public sealed class RegistrationForm
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Full name must be 2–80 characters.")]
    [RegularExpression(PersonRules.NamePattern, ErrorMessage = PersonRules.NameMessage)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(254, ErrorMessage = "Email is too long.")]
    [RegularExpression(PersonRules.EmailPattern, ErrorMessage = PersonRules.EmailMessage)]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9()\-\s]{7,20}$", ErrorMessage = "Enter a valid phone number (7–20 digits; +, spaces, dashes and parentheses allowed).")]
    public string? Phone { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the event terms to register.")]
    public bool AcceptTerms { get; set; }

    /// <summary>Keep the name and email in the session so later forms are pre-filled.</summary>
    public bool RememberMe { get; set; } = true;
}
