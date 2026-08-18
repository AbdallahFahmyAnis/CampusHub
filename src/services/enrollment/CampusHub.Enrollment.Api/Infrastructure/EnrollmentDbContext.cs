using CampusHub.Enrollment.Api.Domain;
using Microsoft.EntityFrameworkCore;
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
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ProcessedAt);
        });
    }
}
