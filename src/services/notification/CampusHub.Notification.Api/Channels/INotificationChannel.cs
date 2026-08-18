using CampusHub.Notification.Api.Domain;

namespace CampusHub.Notification.Api.Channels;

public interface INotificationChannel
{
    string Name { get; }
    Task DeliverAsync(UserNotification notification, CancellationToken ct);
}
