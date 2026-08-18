using CampusHub.Enrollment.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using EnrollmentEntity = CampusHub.Enrollment.Api.Domain.Enrollment;

namespace CampusHub.Enrollment.Api.Infrastructure;

public sealed class EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : DbContext(options)
{
    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EnrollmentEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.StudentId, x.CourseId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.Status });
            entity.Property(x => x.TenantId).HasConversion(GuidAsText);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ProcessedAt);
        });
    }

    private static readonly ValueConverter<Guid, string> GuidAsText = new(
        id => id.ToString("D"),
        value => ParseGuid(value));

    private static Guid ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
}
