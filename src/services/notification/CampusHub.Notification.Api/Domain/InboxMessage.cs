namespace CampusHub.Notification.Api.Domain;

public sealed class InboxMessage
{
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}
