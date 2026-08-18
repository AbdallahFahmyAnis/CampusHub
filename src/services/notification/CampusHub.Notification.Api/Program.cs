using CampusHub.Notification.Api;
using CampusHub.Notification.Api.Features;
using CampusHub.Notification.Api.Infrastructure;
using CampusHub.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNotificationInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseServiceDefaults();
app.UseApiExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapNotificationEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
