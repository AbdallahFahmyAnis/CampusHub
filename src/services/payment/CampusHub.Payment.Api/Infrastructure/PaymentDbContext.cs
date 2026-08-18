using CampusHub.Payment.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Payment.Api.Infrastructure;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentIntent> Payments => Set<PaymentIntent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentIntent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EnrollmentId).IsUnique();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
        });
    }
}
