using CampusHub.Payment.Api;
using CampusHub.Payment.Api.Features;
using CampusHub.Payment.Api.Infrastructure;
using CampusHub.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPaymentInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseServiceDefaults();
app.UseApiExceptionHandler();
app.UseRouting();
app.MapDefaultEndpoints();
app.MapPaymentEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
