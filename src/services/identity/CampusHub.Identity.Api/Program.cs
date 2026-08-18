using CampusHub.Identity.Api;
using CampusHub.Identity.Api.Data;
using CampusHub.Identity.Api.Features;
using CampusHub.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddIdentityInfrastructure();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseServiceDefaults();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapRazorPages();
app.MapControllers();
app.MapIdentityUserEndpoints();
app.MapTenantEndpoints();
app.MapCampusEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync(app.Lifetime.ApplicationStopping);
}

app.Run();
