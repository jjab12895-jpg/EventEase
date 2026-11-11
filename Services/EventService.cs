using EventEase.Models;

namespace EventEase.Services;

/// <summary>
/// In-memory event store with mock data. Registered as a singleton in Program.cs,
/// so edits made on one page are visible on the others.
/// </summary>
public class EventService
{
    private readonly List<EventItem> _events =
    [
        new() { Id = 1, Name = "Tech Innovators Summit", Date = new DateTime(2026, 12, 3),
                Location = "San Pedro Sula, Honduras",
                Description = "A one-day conference on cloud, AI and modern web development." },
        new() { Id = 2, Name = "Annual Company Gala", Date = new DateTime(2026, 12, 12),
                Location = "Tegucigalpa, Honduras",
                Description = "Year-end celebration with dinner, awards and live music." },
        new() { Id = 3, Name = "Leadership Workshop", Date = new DateTime(2027, 1, 20),
                Location = "Online",
                Description = "Hands-on workshop for team leads: feedback, delegation and planning." },
        new() { Id = 4, Name = "Spring Networking Mixer", Date = new DateTime(2027, 3, 15),
                Location = "Roatán, Honduras",
                Description = "An informal evening to meet partners and clients." }
    ];

    public IReadOnlyList<EventItem> GetAll() => _events.OrderBy(e => e.Date).ToList();

    public EventItem? GetById(int id) => _events.FirstOrDefault(e => e.Id == id);
}
