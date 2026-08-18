using CampusHub.BuildingBlocks.Security;
using CampusHub.Enrollment.Api.Infrastructure;
using CampusHub.Enrollment.Api.Sagas;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CampusHub.Enrollment.Api;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddEnrollmentInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("enrollment") ?? "Data Source=enrollment.db";
        builder.Services.AddDbContext<EnrollmentDbContext>(options =>
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
                    ValidAudiences = ["enrollment-api", Clients.Gateway]
                };
            });
        builder.Services.AddAuthorization();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient<CatalogGateway>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Catalog:BaseUrl"] ?? "http://localhost:5102");
        });
        builder.Services.AddHttpClient<PaymentGateway>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Payment:BaseUrl"] ?? "http://localhost:5104");
        });
        builder.Services.AddHttpClient("notification", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Notification:BaseUrl"] ?? "http://localhost:5105");
        });
        builder.Services.AddHttpClient("access", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Access:BaseUrl"] ?? "http://localhost:5106");
        });

        builder.Services.AddScoped<EnrollmentSaga>();
        builder.Services.AddHostedService<OutboxDispatcher>();
        return builder;
    }
}
