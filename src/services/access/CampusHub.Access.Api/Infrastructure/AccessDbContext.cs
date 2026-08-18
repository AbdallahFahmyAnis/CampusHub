using CampusHub.Access.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Access.Api.Infrastructure;

public sealed class AccessDbContext(DbContextOptions<AccessDbContext> options) : DbContext(options)
{
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<AccessCredential> Credentials => Set<AccessCredential>();
    public DbSet<AttendanceScan> Scans => Set<AttendanceScan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.Type).HasMaxLength(128);
        });

        modelBuilder.Entity<AccessCredential>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EnrollmentId).IsUnique();
            entity.HasIndex(x => x.StudentId);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Kind).HasMaxLength(32);
            entity.Property(x => x.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<AttendanceScan>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CourseId);
            entity.HasIndex(x => x.CredentialId);
        });
    }
}
