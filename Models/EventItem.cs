namespace EventEase.Models;

/// <summary>
/// Simple data model for an event shown in the EventEase app.
/// </summary>
public class EventItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
