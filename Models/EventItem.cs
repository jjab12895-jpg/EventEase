using System.ComponentModel.DataAnnotations;

namespace EventEase.Models;

/// <summary>
/// Data model for an event. DataAnnotations rules are enforced by the EventCard edit form,
/// so invalid data is never written back to the shared event.
/// </summary>
public class EventItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Event name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Event name must be 3–100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date is required.")]
    [NotInPast]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(100, ErrorMessage = "Location can't exceed 100 characters.")]
    public string Location { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description can't exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Returns a copy, used as an editable draft.</summary>
    public EventItem Clone() => (EventItem)MemberwiseClone();

    /// <summary>Copies the editable fields from a validated draft (trimming whitespace).</summary>
    public void CopyFrom(EventItem source)
    {
        Name = source.Name.Trim();
        Date = source.Date.Date;
        Location = source.Location.Trim();
        Description = source.Description.Trim();
    }
}

/// <summary>Rejects dates earlier than today.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotInPastAttribute : ValidationAttribute
{
    public NotInPastAttribute() : base("Event date can't be in the past.") { }

    public override bool IsValid(object? value) =>
        value is DateTime date && date.Date >= DateTime.Today;
}
