using EventEase.Models;

namespace EventEase.Services;

/// <summary>
/// Stores registrations and check-ins (in memory, singleton) and powers the Attendance Tracker.
/// Shared by every connected user, so all access is synchronized and callers only ever
/// receive immutable snapshots. <see cref="Changed"/> lets open pages refresh in real time.
/// </summary>
public sealed class AttendanceService
{
    private readonly EventService _events;
    private readonly ILogger<AttendanceService> _logger;
    private readonly object _gate = new();
    private readonly Dictionary<int, List<Registration>> _byEvent = [];
    private int _nextId;

    public AttendanceService(EventService events, ILogger<AttendanceService> logger)
    {
        _events = events;
        _logger = logger;
        _events.DatasetChanged += Reset;
        Reset();
    }

    /// <summary>Raised after any registration, cancellation or check-in.</summary>
    public event Action? Changed;

    public RegistrationResult Register(int eventId, RegistrationForm form)
    {
        var ev = _events.GetById(eventId);
        if (ev is null) return new(RegistrationStatus.EventNotFound);
        if (ev.IsPast) return new(RegistrationStatus.RegistrationClosed);

        var email = form.Email.Trim().ToLowerInvariant();
        Registration registration;

        lock (_gate)
        {
            var list = GetOrCreate(eventId);
            if (list.Any(r => r.Email == email)) return new(RegistrationStatus.AlreadyRegistered);
            if (list.Count >= ev.Capacity) return new(RegistrationStatus.EventFull);

            registration = new Registration(++_nextId, eventId, form.FullName.Trim(), email,
                string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim(), DateTime.Now);
            list.Add(registration);
        }

        _logger.LogInformation("Registration {Code} created for event {EventId}", registration.Code, eventId);
        Changed?.Invoke();
        return new(RegistrationStatus.Success, registration);
    }

    public bool Cancel(int registrationId, string email)
    {
        bool removed;
        lock (_gate)
        {
            removed = _byEvent.Values.Any(list =>
                list.RemoveAll(r => r.Id == registrationId && r.Email == email.ToLowerInvariant()) > 0);
        }
        if (removed) Changed?.Invoke();
        return removed;
    }

    /// <summary>Marks a registration as checked in (or undoes it).</summary>
    public bool SetCheckedIn(int registrationId, bool checkedIn)
    {
        var found = false;
        lock (_gate)
        {
            foreach (var list in _byEvent.Values)
            {
                var index = list.FindIndex(r => r.Id == registrationId);
                if (index < 0) continue;

                list[index] = list[index] with { CheckedInAt = checkedIn ? DateTime.Now : null };
                found = true;
                break;
            }
        }
        if (found) Changed?.Invoke();
        return found;
    }

    public IReadOnlyList<Registration> GetForEvent(int eventId)
    {
        lock (_gate)
        {
            return _byEvent.TryGetValue(eventId, out var list)
                ? list.OrderBy(r => r.FullName, StringComparer.CurrentCultureIgnoreCase).ToList()
                : [];
        }
    }

    public IReadOnlyList<Registration> GetForEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return [];
        var key = email.Trim().ToLowerInvariant();
        lock (_gate)
        {
            return _byEvent.Values.SelectMany(l => l).Where(r => r.Email == key)
                .OrderBy(r => r.RegisteredAt).ToList();
        }
    }

    public int CountForEvent(int eventId)
    {
        lock (_gate)
        {
            return _byEvent.TryGetValue(eventId, out var list) ? list.Count : 0;
        }
    }

    public bool IsRegistered(int eventId, string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var key = email.Trim().ToLowerInvariant();
        lock (_gate)
        {
            return _byEvent.TryGetValue(eventId, out var list) && list.Any(r => r.Email == key);
        }
    }

    /// <summary>Attendance numbers for every event that has at least one registration.</summary>
    public IReadOnlyList<AttendanceStats> GetSummary()
    {
        lock (_gate)
        {
            return _byEvent.Where(kv => kv.Value.Count > 0)
                .Select(kv => new AttendanceStats(kv.Key, kv.Value.Count, kv.Value.Count(r => r.CheckedIn)))
                .ToList();
        }
    }

    private List<Registration> GetOrCreate(int eventId)
    {
        if (!_byEvent.TryGetValue(eventId, out var list))
        {
            list = [];
            _byEvent[eventId] = list;
        }
        return list;
    }

    // Demo data: a few fictional attendees on the first events so the tracker isn't empty.
    private static readonly (string Name, string Email)[] DemoPeople =
    [
        ("Ana López", "ana.lopez@example.com"), ("Carlos Mejía", "carlos.mejia@example.com"),
        ("Lucía Pineda", "lucia.pineda@example.com"), ("Mario Zelaya", "mario.zelaya@example.com"),
        ("Sofía Reyes", "sofia.reyes@example.com"), ("Diego Castro", "diego.castro@example.com")
    ];

    private void Reset()
    {
        lock (_gate)
        {
            _byEvent.Clear();
            _nextId = 0;

            int[] seedCounts = [6, 3, 2, 4];
            var firstEvents = _events.GetAll().Where(e => e.Id <= seedCounts.Length);
            foreach (var ev in firstEvents)
            {
                var list = GetOrCreate(ev.Id);
                var count = Math.Min(seedCounts[ev.Id - 1], ev.Capacity);
                for (var i = 0; i < count; i++)
                {
                    var (name, email) = DemoPeople[i];
                    var checkedIn = i % 2 == 0 ? DateTime.Now.AddMinutes(-i) : (DateTime?)null;
                    list.Add(new Registration(++_nextId, ev.Id, name, email, null, DateTime.Now.AddDays(-i - 1), checkedIn));
                }
            }
        }
        Changed?.Invoke();
    }
}
