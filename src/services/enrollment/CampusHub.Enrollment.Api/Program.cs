using CampusHub.Enrollment.Api;
using CampusHub.Enrollment.Api.Features;
using CampusHub.Enrollment.Api.Infrastructure;
using CampusHub.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddEnrollmentInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseServiceDefaults();
app.UseApiExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapEnrollmentEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
