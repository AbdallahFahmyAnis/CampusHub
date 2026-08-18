namespace CampusHub.Notification.Api.Domain;

public sealed class UserNotification
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Channel { get; set; } = NotificationChannels.InApp;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = NotificationStatus.Queued;
    public bool Read { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public static class NotificationChannels
{
    public const string InApp = "InApp";
    public const string Email = "Email";
    public const string Sms = "Sms";
    public const string Push = "Push";
}

public static class NotificationStatus
{
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Stubbed = "Stubbed";
    public const string Failed = "Failed";
}
