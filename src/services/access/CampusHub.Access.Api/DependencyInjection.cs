using CampusHub.Access.Api.Features;
using CampusHub.Access.Api.Infrastructure;
using CampusHub.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CampusHub.Access.Api;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddAccessInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("access") ?? "Data Source=access.db";
        builder.Services.AddDbContext<AccessDbContext>(options =>
        {
            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
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
                    ValidAudiences = ["access-api", Clients.Gateway]
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<CredentialSigner>();
        builder.Services.AddScoped<AccessEventProcessor>();
        return builder;
    }
}
