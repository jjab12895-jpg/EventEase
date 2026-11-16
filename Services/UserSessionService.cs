using System.Security.Cryptography;
using EventEase.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace EventEase.Services;

/// <summary>
/// User Session Tracker (scoped = one instance per browser tab / Blazor circuit).
/// State is kept in memory for speed and mirrored to the browser's sessionStorage
/// (encrypted by ASP.NET Core Data Protection), so it survives page reloads and
/// reconnects for as long as the tab is open.
/// </summary>
public sealed class UserSessionService(ProtectedSessionStorage storage, ILogger<UserSessionService> logger)
{
    private const string StorageKey = "eventease.session";
    private const int MaxRecent = 5;

    private Task? _initTask;
    private List<int> _recentlyViewed = [];

    /// <summary>Raised whenever the session changes, so components can re-render.</summary>
    public event Action? Changed;

    public UserProfile? CurrentUser { get; private set; }
    public DateTime StartedAt { get; private set; } = DateTime.Now;
    public bool IsLoaded { get; private set; }
    public bool IsSignedIn => CurrentUser is not null;

    /// <summary>Most recent first.</summary>
    public IReadOnlyList<int> RecentlyViewed => _recentlyViewed;

    /// <summary>
    /// Loads the saved session once. Browser storage needs JS interop, which isn't available
    /// during prerendering, so call this from OnAfterRenderAsync. Safe to call many times.
    /// </summary>
    public Task InitializeAsync() => _initTask ??= LoadAsync();

    public async Task SignInAsync(UserProfile profile)
    {
        await InitializeAsync();
        CurrentUser = profile.Normalized();
        await SaveAsync();
    }

    public async Task SignOutAsync()
    {
        await InitializeAsync();
        CurrentUser = null;
        _recentlyViewed = [];
        StartedAt = DateTime.Now;
        try
        {
            await storage.DeleteAsync(StorageKey);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Could not clear session storage");
        }
        Changed?.Invoke();
    }

    public async Task TrackViewAsync(int eventId)
    {
        await InitializeAsync();
        if (_recentlyViewed.FirstOrDefault() == eventId) return;

        _recentlyViewed.Remove(eventId);
        _recentlyViewed.Insert(0, eventId);
        if (_recentlyViewed.Count > MaxRecent) _recentlyViewed.RemoveRange(MaxRecent, _recentlyViewed.Count - MaxRecent);
        await SaveAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var result = await storage.GetAsync<SessionState>(StorageKey);
            if (result.Success && result.Value is { } saved)
            {
                CurrentUser = saved.User;
                StartedAt = saved.StartedAt;
                _recentlyViewed = saved.RecentlyViewed?.Take(MaxRecent).ToList() ?? [];
            }
        }
        catch (CryptographicException ex)
        {
            // Data written with an old key (e.g. after the app restarted) – start a fresh session.
            logger.LogInformation(ex, "Saved session could not be decrypted; starting a new one");
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Session storage unavailable; continuing with an in-memory session");
        }

        IsLoaded = true;
        Changed?.Invoke();
    }

    private async Task SaveAsync()
    {
        try
        {
            await storage.SetAsync(StorageKey, new SessionState(CurrentUser, StartedAt, _recentlyViewed));
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            // The in-memory session still works; it just won't survive a reload.
            logger.LogWarning(ex, "Could not persist session state");
        }
        Changed?.Invoke();
    }

    private sealed record SessionState(UserProfile? User, DateTime StartedAt, List<int>? RecentlyViewed);
}
