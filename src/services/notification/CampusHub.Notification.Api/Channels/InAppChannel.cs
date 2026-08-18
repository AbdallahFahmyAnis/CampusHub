using CampusHub.Notification.Api.Domain;

namespace CampusHub.Notification.Api.Channels;

public sealed class InAppChannel : INotificationChannel
{
    public string Name => NotificationChannels.InApp;

    public Task DeliverAsync(UserNotification notification, CancellationToken ct)
    {
        notification.Status = NotificationStatus.Sent;
        return Task.CompletedTask;
    }
}
