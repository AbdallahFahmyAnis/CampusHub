using CampusHub.BuildingBlocks.Security;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CampusHub.Catalog.Api;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("catalog") ?? "Data Source=catalog.db";

        builder.Services.AddDbContext<CatalogDbContext>(options =>
        {
            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        var authority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5101";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = false;
                var metadata = builder.Configuration["Identity:MetadataAddress"];
                if (!string.IsNullOrWhiteSpace(metadata))
                {
                    options.MetadataAddress = metadata;
                }
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ValidateAudience = true,
                    ValidAudiences = ["catalog-api", Clients.Gateway]
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CanManageCatalog", policy =>
                policy.RequireRole(Roles.Teacher, Roles.Administrator));
        });

        builder.Services.AddScoped<CatalogSeeder>();
        builder.Services.AddHttpClient<EnrollmentGateway>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Enrollment:BaseUrl"] ?? "http://localhost:5103");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        return builder;
    }
}
