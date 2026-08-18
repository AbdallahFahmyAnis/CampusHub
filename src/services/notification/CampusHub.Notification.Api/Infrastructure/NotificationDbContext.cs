using CampusHub.Notification.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Notification.Api.Infrastructure;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<UserNotification> Notifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.Type).HasMaxLength(128);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => x.EventId);
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Channel).HasMaxLength(32);
            entity.Property(x => x.Status).HasMaxLength(32);
        });
    }
}
