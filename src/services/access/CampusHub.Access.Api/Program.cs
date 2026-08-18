using CampusHub.Access.Api;
using CampusHub.Access.Api.Features;
using CampusHub.Access.Api.Infrastructure;
using CampusHub.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAccessInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseServiceDefaults();
app.UseApiExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapAccessEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
