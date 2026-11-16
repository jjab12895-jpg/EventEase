# EventEase

A Blazor web app (.NET 8, Interactive Server) for browsing corporate and social events, registering, and tracking attendance.

## Features

- **Events:** a searchable list of reusable `EventCard` components with inline editing (validated, two-way binding) and virtualized rendering for large lists.
- **Event details:** seats taken and seats left, your own registration status, and links to register and to the attendance view.
- **Registration Form:** client-side DataAnnotations validation plus server-side rules: duplicate email, event full, registration closed. It also has double-submit protection.
- **User Session Tracker:** sign in with your name and email. The session follows you across pages and page reloads (encrypted browser `sessionStorage`), pre-fills forms, and remembers recently viewed events.
- **My registrations:** your registrations, with inline cancel confirmation.
- **Attendance Tracker:** totals, attendance rate, per-event fill and check-in, and door check-in with attendee search. It updates live for every connected user.
- **Reliability:** a friendly 404 for unknown routes (real HTTP 404), "event not found" handling, and an `ErrorBoundary` around every page.

## Run locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet watch          # dev mode with hot reload (opens the browser)
# or
dotnet run            # http://localhost:5287
```

In Development, the Events page also shows the performance-testing tools from Activity 2: a dataset size selector (4 / 1,000 / 10,000 events), a virtualization toggle and a render timer.

## Deploy

```bash
dotnet publish -c Release -o ./publish
cd publish
ASPNETCORE_ENVIRONMENT=Production dotnet EventEase.dll --urls http://0.0.0.0:8080
```

The output (~1 MB) runs on any host with the ASP.NET Core 8 runtime. That includes Azure App Service; `web.config` is generated for IIS.

**Before a real production deployment:**

- Data lives in memory (`EventService`, `AttendanceService`) and resets when the app restarts. Replace it with a database, for example EF Core + Azure SQL, behind the same service methods.
- Sessions are encrypted with ASP.NET Core Data Protection. On multiple servers, or to keep sessions across restarts, persist the keys (for example `PersistKeysToAzureBlobStorage`).
- "Sign in" only identifies the user for convenience; it is not authentication. Add ASP.NET Core Identity or Microsoft Entra ID before exposing organizer features such as check-in.
- Blazor Server keeps a live connection per user. With more than one server, enable sticky sessions or use Azure SignalR Service.

## Project structure

```
Models/
  EventItem.cs           event + validation rules ([NotInPast], capacity)
  FormModels.cs          RegistrationForm, UserProfile (shared name/email rules)
  Registration.cs        Registration record, RegistrationResult, AttendanceStats
Services/
  EventService.cs        events (singleton, cached + O(1) lookups)
  AttendanceService.cs   registrations/check-ins (singleton, thread-safe, Changed event)
  UserSessionService.cs  per-tab session state (scoped, ProtectedSessionStorage)
Components/
  Shared/   EventCard, EventRow, SessionBar, NotFoundMessage
  Pages/    Home, Events, EventDetails, Register, MyRegistrations, Profile,
            Attendance, EventAttendance, NotFound, Error
  Layout/   MainLayout (ErrorBoundary + SessionBar), NavMenu
```

There are no NuGet package dependencies: the app uses only the ASP.NET Core shared framework.

See [TESTING.md](TESTING.md) for test results and [COPILOT_SUMMARY.md](COPILOT_SUMMARY.md) for how Copilot was used.
