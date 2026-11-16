namespace EventEase.Models;

/// <summary>
/// One person's registration for one event. Immutable: check-in creates a new copy,
/// so lists handed to the UI can never be changed underneath it by another user.
/// </summary>
public sealed record Registration(
    int Id,
    int EventId,
    string FullName,
    string Email,
    string? Phone,
    DateTime RegisteredAt,
    DateTime? CheckedInAt = null)
{
    public bool CheckedIn => CheckedInAt is not null;

    /// <summary>Short confirmation code shown to the attendee, e.g. EE-000042.</summary>
    public string Code => $"EE-{Id:D6}";
}

/// <summary>Outcome of a registration attempt, so the form can show a specific, friendly error.</summary>
public enum RegistrationStatus
{
    Success,
    EventNotFound,
    RegistrationClosed,
    EventFull,
    AlreadyRegistered
}

public sealed record RegistrationResult(RegistrationStatus Status, Registration? Registration = null);

/// <summary>Per-event attendance numbers used by the Attendance Tracker.</summary>
public sealed record AttendanceStats(int EventId, int Registered, int CheckedIn);
