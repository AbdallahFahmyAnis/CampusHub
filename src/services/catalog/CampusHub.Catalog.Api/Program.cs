using CampusHub.Catalog.Api;
using CampusHub.Catalog.Api.Features;
using CampusHub.Catalog.Api.Infrastructure;
using CampusHub.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddCatalogInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseServiceDefaults();
app.UseApiExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapCatalogEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<CatalogSeeder>();
    await seeder.SeedAsync(app.Lifetime.ApplicationStopping);
}

app.Run();
