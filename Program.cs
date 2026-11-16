using System.Globalization;
using EventEase.Components;
using EventEase.Services;

var builder = WebApplication.CreateBuilder(args);

// Consistent date/number formatting regardless of the server's OS culture.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Shared, in-memory data for all users (replace with a database for real deployments).
builder.Services.AddSingleton<EventService>();
builder.Services.AddSingleton<AttendanceService>();

// One per browser tab (Blazor circuit): the user's session state.
builder.Services.AddScoped<UserSessionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
