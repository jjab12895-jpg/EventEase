using EventEase.Models;

namespace EventEase.Services;

/// <summary>
/// In-memory event store with mock data (singleton).
/// Performance fixes vs. Activity 1:
///  - the sorted list is cached instead of re-sorted and copied on every GetAll() call;
///  - lookups by id use a dictionary (O(1)) instead of a linear search.
/// UseDataset is a development/performance-testing aid; the selector is only shown in Development.
/// </summary>
public class EventService
{
    private static readonly EventItem[] SampleEvents =
    [
        new() { Id = 1, Name = "Tech Innovators Summit", Date = new DateTime(2026, 12, 3),
                Location = "San Pedro Sula, Honduras", Capacity = 150,
                Description = "A one-day conference on cloud, AI and modern web development." },
        new() { Id = 2, Name = "Annual Company Gala", Date = new DateTime(2026, 12, 12),
                Location = "Tegucigalpa, Honduras", Capacity = 300,
                Description = "Year-end celebration with dinner, awards and live music." },
        new() { Id = 3, Name = "Leadership Workshop", Date = new DateTime(2027, 1, 20),
                Location = "Online", Capacity = 25,
                Description = "Hands-on workshop for team leads: feedback, delegation and planning." },
        new() { Id = 4, Name = "Spring Networking Mixer", Date = new DateTime(2027, 3, 15),
                Location = "Roatán, Honduras", Capacity = 5,
                Description = "An informal evening to meet partners and clients." }
    ];

    private static readonly string[] Kinds = ["Conference", "Workshop", "Gala", "Meetup", "Webinar", "Expo", "Seminar"];
    private static readonly string[] Cities = ["Tegucigalpa", "San Pedro Sula", "La Ceiba", "Roatán", "Copán Ruinas", "Online"];

    private List<EventItem> _sorted = [];
    private Dictionary<int, EventItem> _byId = [];

    public EventService() => UseDataset(SampleEvents.Length);

    /// <summary>Raised after a different dataset is loaded (used by AttendanceService to reset).</summary>
    public event Action? DatasetChanged;

    /// <summary>Number of events currently loaded.</summary>
    public int Count => _sorted.Count;

    /// <summary>
    /// Loads the 4 sample events, or generates <paramref name="size"/> mock events
    /// to test performance with large data sets.
    /// </summary>
    public void UseDataset(int size)
    {
        var events = size <= SampleEvents.Length
            ? SampleEvents.Select(e => e.Clone()).ToList()
            : Generate(size);

        _sorted = events.OrderBy(e => e.Date).ThenBy(e => e.Id).ToList();
        _byId = _sorted.ToDictionary(e => e.Id);
        DatasetChanged?.Invoke();
    }

    public IReadOnlyList<EventItem> GetAll() => _sorted;

    public EventItem? GetById(int id) => _byId.GetValueOrDefault(id);

    private static List<EventItem> Generate(int count)
    {
        var rng = new Random(42); // fixed seed -> repeatable data
        var start = DateTime.Today.AddDays(1);
        var list = new List<EventItem>(count);
        for (var i = 1; i <= count; i++)
        {
            var kind = Kinds[rng.Next(Kinds.Length)];
            var city = Cities[rng.Next(Cities.Length)];
            list.Add(new EventItem
            {
                Id = i,
                Name = $"{kind} #{i}",
                Date = start.AddDays(rng.Next(0, 730)),
                Location = city == "Online" ? city : $"{city}, Honduras",
                Capacity = rng.Next(2, 51) * 10,
                Description = $"Generated {kind.ToLowerInvariant()} used for performance testing."
            });
        }
        return list;
    }
}
