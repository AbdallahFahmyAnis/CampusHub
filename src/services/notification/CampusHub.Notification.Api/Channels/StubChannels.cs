using CampusHub.Notification.Api.Domain;

namespace CampusHub.Notification.Api.Channels;

public sealed class SmsChannel(ILogger<SmsChannel> logger) : INotificationChannel
{
    public string Name => NotificationChannels.Sms;

    public Task DeliverAsync(UserNotification notification, CancellationToken ct)
    {
        logger.LogInformation("SMS stub for {UserId}: {Title}", notification.UserId, notification.Title);
        notification.Status = NotificationStatus.Stubbed;
        return Task.CompletedTask;
    }
}

public sealed class PushChannel(ILogger<PushChannel> logger) : INotificationChannel
{
    public string Name => NotificationChannels.Push;

    public Task DeliverAsync(UserNotification notification, CancellationToken ct)
    {
        logger.LogInformation("Push stub for {UserId}: {Title}", notification.UserId, notification.Title);
        notification.Status = NotificationStatus.Stubbed;
        return Task.CompletedTask;
    }
}
