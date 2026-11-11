# EventEase – Activity 2: Debugging & Optimization

## Issues found (Activity 1 build)

| # | Issue | How it showed up |
|---|-------|------------------|
| 1 | No input validation in `EventCard` | Clearing the name and typing spaces in the location was saved: the card showed an empty title. |
| 2 | Invalid routes not handled | `/no-existe` and `/events/abc` returned a blank browser 404 with no navigation. |
| 3 | Event list renders every card | All events were rendered at once; `GetAll()` re-sorted and copied the list on every call, and `GetById()` was a linear search. |
| 4 | `ValidationMessage class="small"` (found while testing the fix) | Passing `class` replaced the built-in `validation-message` class, so errors lost their red styling. |

## Fixes

**Validation – `Models/EventItem.cs`, `Components/Shared/EventCard.razor`**
- DataAnnotations: name required, 3–100 chars; location required, max 100 chars; description max 500 chars; custom `[NotInPast]` date rule.
- The card edits a **draft copy** inside an `EditForm` with `DataAnnotationsValidator`. The bound event is updated (and `EventChanged` raised) only in `OnValidSubmit`, so invalid data never reaches the rest of the app. **Cancel** discards the draft.
- Saved values are trimmed. A `null` event renders an "unavailable" card instead of throwing.

**Routing – `Pages/NotFound.razor`, `Shared/NotFoundMessage.razor`, `Layout/MainLayout.razor`**
- Catch-all route `@page "/{*path:nonfile}"` shows a friendly page with links back. Literal and constrained routes always win over a catch-all, so it never hides a real page.
- Unknown URLs return a real **HTTP 404** status during prerendering.
- `/events/{id}` and `/events/{id}/register` with an unknown id show "Event not found" with navigation.
- An `ErrorBoundary` in the layout catches unexpected exceptions in a page, offers "Try again", and resets when the user navigates.

**Performance – `Pages/Events.razor`, `Shared/EventRow.razor`, `Services/EventService.cs`**
- `<Virtualize>` renders only the rows near the viewport. Rows of 3 cards are virtualized, so the responsive grid still works.
- Filtering runs only when the search or dataset changes, not on every render.
- The service caches the sorted list and uses a `Dictionary` for O(1) lookups by id.
- A dataset selector (4 / 1,000 / 10,000 events), a virtualization toggle and a render timer on the page make the improvement measurable.

## Test results

Automated browser run (Playwright, Chromium, 1280×900) against `dotnet run`.

### Data binding / validation

| Case | Result |
|------|--------|
| Empty name + whitespace-only location | "Event name is required." · "Location is required." – not saved ✅ |
| Name "ab" | "Event name must be 3–100 characters." ✅ |
| Name with 101 chars | "Event name must be 3–100 characters." ✅ |
| Date 2020-01-01 | "Event date can't be in the past." ✅ |
| Cancel after invalid edits | Original name kept ✅ |
| Valid save with padded text `"  Tech Summit 2026  "` | Saved trimmed; parent page notified via `EventChanged` ✅ |

### Routing

| URL | Result |
|-----|--------|
| `/no-existe` | 404 · "Page not found" ✅ |
| `/events/abc` (non-numeric id) | 404 · "Page not found" ✅ |
| `/events/1/xyz` | 404 · "Page not found" ✅ |
| `/events/99`, `/events/99/register` | "Event not found" with links back ✅ |
| Client-side navigation to `/una/ruta/mala` | "Page not found" without reload ✅ |
| `/missing.css` | Plain 404 (files are not caught by the page route) ✅ |
| Browser JS errors during the whole run | none ✅ |

### Performance – 10,000 events

| | Without virtualization | With virtualization |
|---|---|---|
| Time from selecting the dataset to the list on screen | **2,190 ms** | **197 ms** (~11× faster) |
| Event cards in the DOM | 10,000 | 42 |
| Total DOM elements | 123,387 | 573 |

- Scrolling to the end of 10,000 events keeps 42 cards in the DOM.
- Searching "Gala" in 10,000 events: 38 ms, 1,401 results.
- `/events/5000` (generated event) opens instantly thanks to the dictionary lookup.

The "last render" figure on the page is server-side render and diff time. The end-to-end numbers above also include sending the update and building the DOM in the browser, which is where the non-virtualized list spends its time.

## Copilot prompts used / suggested
- "Find bugs in EventCard.razor: what happens with empty or invalid input?"
- "Add DataAnnotations validation to EventItem and use an EditForm in EventCard so only valid data updates the bound event."
- "How do I handle unknown routes in a .NET 8 Blazor Web App with interactive server rendering?"
- "Optimize this list of 10,000 EventCard components using Virtualize."
