using CampusHub.BuildingBlocks.Security;
using CampusHub.Notification.Api.Channels;
using CampusHub.Notification.Api.Features;
using CampusHub.Notification.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CampusHub.Notification.Api;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddNotificationInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("notification") ?? "Data Source=notification.db";
        builder.Services.AddDbContext<NotificationDbContext>(options =>
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
                    ValidAudiences = ["notification-api", Clients.Gateway]
                };
            });
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<INotificationChannel, InAppChannel>();
        builder.Services.AddScoped<INotificationChannel, EmailChannel>();
        builder.Services.AddScoped<INotificationChannel, SmsChannel>();
        builder.Services.AddScoped<INotificationChannel, PushChannel>();
        builder.Services.AddScoped<NotificationProcessor>();
        builder.Services.AddSingleton<NotificationBus>();
        return builder;
    }
}
