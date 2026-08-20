using CampusHub.Enrollment.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using EnrollmentEntity = CampusHub.Enrollment.Api.Domain.Enrollment;

namespace CampusHub.Enrollment.Api.Infrastructure;

public sealed class EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : DbContext(options)
{
    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();
    public DbSet<CourseWaitlist> CourseWaitlists => Set<CourseWaitlist>();
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

        modelBuilder.Entity<CourseWaitlist>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.TenantId).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.CourseTitle).HasMaxLength(300);
            entity.Property(x => x.StudentId).HasMaxLength(64);
            entity.Property(x => x.StudentEmail).HasMaxLength(320);
            entity.Property(x => x.StudentName).HasMaxLength(200);
            entity.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.StudentId });
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
