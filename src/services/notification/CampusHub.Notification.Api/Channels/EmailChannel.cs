using CampusHub.Notification.Api.Domain;

namespace CampusHub.Notification.Api.Channels;

/// <summary>
/// Dev stand-in for SMTP/SendGrid. Marks the notification sent after logging the would-be email.
/// </summary>
public sealed class EmailChannel(ILogger<EmailChannel> logger) : INotificationChannel
{
    public string Name => NotificationChannels.Email;

    public Task DeliverAsync(UserNotification notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Email to {Email}: {Title} — {Body}",
            notification.UserEmail,
            notification.Title,
            notification.Body);
        notification.Status = string.IsNullOrWhiteSpace(notification.UserEmail)
            ? NotificationStatus.Failed
            : NotificationStatus.Sent;
        return Task.CompletedTask;
    }
}
