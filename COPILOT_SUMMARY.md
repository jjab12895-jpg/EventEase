# How Copilot assisted in building EventEase

> **Draft:** before you submit, run each prompt in Copilot (Visual Studio / VS Code), compare its answer with the final code, and edit this text so it describes what Copilot actually suggested in your session.

## Activity 1 – Foundation

| Step | Copilot prompt | Contribution → what ended up in the code |
|---|---|---|
| Event Card structure | "Create a Blazor component EventCard that displays an event's name, date and location using Bootstrap card markup." | Card skeleton and `[Parameter]` properties → `Components/Shared/EventCard.razor` |
| Data model | "Create a simple EventItem class with Id, Name, Date, Location and Description, and a service with mock data." | `Models/EventItem.cs`, `Services/EventService.cs` |
| Two-way binding | "How do I support @bind-Event on a custom component?" | `Event` + `EventChanged` (`EventCallback<EventItem>`) pattern |
| Routing | "Add routes for an event list, details (/events/{id}) and registration page, with navigation links." | `@page "/events/{Id:int}"` with a type constraint, `NavLink` entries, breadcrumbs; `OnParametersSet` so changing the id reloads data |

## Activity 2 – Debugging & optimization

| Step | Copilot prompt | Contribution → what ended up in the code |
|---|---|---|
| Find bugs | "/explain What happens in EventCard if the user clears the name or types only spaces?" | Identified that empty values were written directly to the shared event |
| Validation | "Add DataAnnotations validation to EventItem and use an EditForm so only valid data updates the bound event." | `EditForm` + `DataAnnotationsValidator` on a draft copy; custom `[NotInPast]` attribute |
| Invalid routes | "How do I show a friendly 404 page for unknown routes in a .NET 8 Blazor Web App?" | Catch-all `@page "/{*path:nonfile}"`, real 404 status, `ErrorBoundary` in the layout |
| Performance | "This list renders 10,000 EventCard components and is slow. How can I optimize it?" | `<Virtualize>`, filtering only on change, cached sort, dictionary lookups |
| Verify | "Generate test cases for empty, too-long and past-date inputs." | Test matrix in `TESTING.md` |

**Correction made by hand:** passing `class="small"` to `ValidationMessage` replaced its built-in `validation-message` class, so errors lost their red style. The fix was to style `.event-card .validation-message` in CSS instead.

## Activity 3 – Advanced features

| Step | Copilot prompt | Contribution → what ended up in the code |
|---|---|---|
| Outline | "Review this project and outline what's needed for a registration form with validation, user session tracking and an attendance tracker." | Feature list → models, services and pages below |
| Registration Form | "Create a RegistrationForm model with validation for full name, email, optional phone and an accept-terms checkbox." | `Models/FormModels.cs` (regex rules shared with `UserProfile`) |
| Server-side errors | "How do I show a server-side error, like a duplicate email, on a specific field in an EditForm?" | `ValidationMessageStore` + `NotifyValidationStateChanged`; error clears on `OnFieldChanged` |
| Session state | "How can I keep user session state in Blazor Server so it survives a page reload?" | Scoped `UserSessionService` + `ProtectedSessionStorage`, loaded in `OnAfterRenderAsync` (no JS interop during prerender) |
| Attendance Tracker | "Create a thread-safe in-memory AttendanceService with register, cancel, check-in and per-event stats, and notify open pages of changes." | `Services/AttendanceService.cs` (lock, immutable `Registration` records, `Changed` event), `Attendance` and `EventAttendance` pages |
| Live updates | "Why do I get 'The current thread is not associated with the Dispatcher' when updating from another user's action?" | `InvokeAsync(StateHasChanged)` in every event handler, plus `IDisposable` to unsubscribe |
| Security review | "/review Are there security issues in the sign-in page's returnUrl handling?" | Only local return URLs are accepted (open-redirect protection) |

## Production readiness (Copilot review)

Prompt: "/review this project for production readiness and unnecessary dependencies."

- **Removed:** the unused `System.Net.Http`, `System.Net.Http.Json` and `Microsoft.JSInterop` imports, the template's Counter and Weather pages and About link, and unused members. The project has **no NuGet packages**.
- **Development-only tools:** the performance-testing tools are shown only when `IWebHostEnvironment.IsDevelopment()`.
- **Hardening:**
  - fixed `en-US` culture
  - structured logging (`ILogger`)
  - `UseExceptionHandler` + HSTS outside Development
  - `.NET 9`-only `Lock` type replaced by `object` for .NET 8 compatibility
  - `SetCheckedIn` returns whether it found the registration
- **Verified:** `dotnet publish -c Release` builds with 0 warnings, and the published app works with `ASPNETCORE_ENVIRONMENT=Production`.

## Takeaways

- Copilot was fastest for scaffolding: components, models, validation attributes and boilerplate such as `EventCallback` and `IDisposable`.
- The `/explain` and `/review` commands helped find problems: unvalidated binding, missing 404 handling and open redirects.
- Every suggestion still needed testing. The `ValidationMessage` class override and the .NET 9-only `Lock` type were found by building and running the app, not by Copilot.
