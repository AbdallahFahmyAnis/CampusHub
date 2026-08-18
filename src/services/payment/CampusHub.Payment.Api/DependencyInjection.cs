using CampusHub.Payment.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Payment.Api;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddPaymentInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("payment") ?? "Data Source=payment.db";
        builder.Services.AddDbContext<PaymentDbContext>(options =>
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

        builder.Services.AddHttpClient("enrollment", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Enrollment:BaseUrl"] ?? "http://localhost:5103");
        });
        builder.Services.AddHostedService<MockPspProcessor>();
        return builder;
    }
}
