using System.Text.Json;
using CampusHub.Contracts.Events;
using CampusHub.Notification.Api.Channels;
using CampusHub.Notification.Api.Domain;
using CampusHub.Notification.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Notification.Api.Features;

public sealed class NotificationProcessor(
    NotificationDbContext db,
    IEnumerable<INotificationChannel> channels,
    NotificationBus bus,
    ILogger<NotificationProcessor> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task HandleAsync(IntegrationEventDto envelope, CancellationToken ct)
    {
        if (await db.Inbox.AnyAsync(x => x.EventId == envelope.EventId, ct))
        {
            return;
        }

        db.Inbox.Add(new InboxMessage
        {
            EventId = envelope.EventId,
            Type = envelope.Type,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        var messages = BuildMessages(envelope);
        foreach (var message in messages)
        {
            var channel = channels.FirstOrDefault(c => c.Name == message.Channel);
            if (channel is null)
            {
                logger.LogWarning("No channel registered for {Channel}", message.Channel);
                message.Status = NotificationStatus.Failed;
            }
            else
            {
                await channel.DeliverAsync(message, ct);
            }

            db.Notifications.Add(message);
        }

        await db.SaveChangesAsync(ct);

        // Signal SSE streams for affected users
        foreach (var m in messages.Where(m => m.Channel == NotificationChannels.InApp))
        {
            bus.Notify(m.UserId, $"{{\"id\":\"{m.Id}\",\"title\":{System.Text.Json.JsonSerializer.Serialize(m.Title)}}}");
        }
    }

    private static List<UserNotification> BuildMessages(IntegrationEventDto envelope)
    {
        return envelope.Type switch
        {
            EventTypes.EnrollmentStarted => FromStarted(envelope),
            EventTypes.EnrollmentConfirmed => FromConfirmed(envelope),
            EventTypes.EnrollmentCancelled => FromCancelled(envelope),
            EventTypes.InviteAccepted => FromInviteAccepted(envelope),
            EventTypes.PlanUpgraded => FromPlanUpgraded(envelope),
            EventTypes.CourseCompleted => FromCourseCompleted(envelope),
            _ => []
        };
    }

    private static List<UserNotification> FromStarted(IntegrationEventDto envelope)
    {
        var body = JsonSerializer.Deserialize<EnrollmentStartedV1>(envelope.Payload, JsonOptions);
        if (body is null)
        {
            return [];
        }

        return
        [
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.InApp,
                "Payment in progress",
                $"We reserved a seat in {body.CourseTitle}. Payment is being processed.")
        ];
    }

    private static List<UserNotification> FromConfirmed(IntegrationEventDto envelope)
    {
        var body = JsonSerializer.Deserialize<EnrollmentConfirmedV1>(envelope.Payload, JsonOptions);
        if (body is null)
        {
            return [];
        }

        var title = "Enrollment confirmed";
        var text = $"You are enrolled in {body.CourseTitle}. Your course pass QR is ready.";
        return
        [
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.InApp, title, text),
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.Email, title, text),
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.Push, title, text)
        ];
    }

    private static List<UserNotification> FromCancelled(IntegrationEventDto envelope)
    {
        var body = JsonSerializer.Deserialize<EnrollmentCancelledV1>(envelope.Payload, JsonOptions);
        if (body is null)
        {
            return [];
        }

        var title = "Enrollment cancelled";
        var text = $"Enrollment in {body.CourseTitle} was cancelled: {body.Reason}";
        return
        [
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.InApp, title, text),
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.Email, title, text)
        ];
    }

    private static List<UserNotification> FromInviteAccepted(IntegrationEventDto envelope)
    {
        var body = JsonSerializer.Deserialize<InviteAcceptedV1>(envelope.Payload, JsonOptions);
        if (body is null)
        {
            return [];
        }

        // Notify the admin who sent the invite
        return
        [
            Create(envelope, body.AdminId, body.AdminEmail, NotificationChannels.InApp,
                "Invite accepted",
                $"{body.InviteeName} ({body.InviteeEmail}) has joined {body.TenantName} as {body.Role}."),
            // Also confirm to the new member
            Create(envelope, body.InviteeId, body.InviteeEmail, NotificationChannels.InApp,
                $"Welcome to {body.TenantName}",
                $"Your invitation has been accepted. You are now a member of {body.TenantName}.")
        ];
    }

    private static List<UserNotification> FromPlanUpgraded(IntegrationEventDto envelope)
    {
        var body = JsonSerializer.Deserialize<PlanUpgradedV1>(envelope.Payload, JsonOptions);
        if (body is null)
        {
            return [];
        }

        return
        [
            Create(envelope, body.AdminId, body.AdminEmail, NotificationChannels.InApp,
                "Plan upgraded",
                $"{body.TenantName} is now on the {body.NewPlan} plan. Sign in again to activate new features.")
        ];
    }

    private static List<UserNotification> FromCourseCompleted(IntegrationEventDto envelope)
    {
        var body = JsonSerializer.Deserialize<CourseCompletedV1>(envelope.Payload, JsonOptions);
        if (body is null)
        {
            return [];
        }

        return
        [
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.InApp,
                "Course completed 🎓",
                $"Congratulations! You've completed {body.CourseTitle}. Your certificate has been issued."),
            Create(envelope, body.StudentId, body.StudentEmail, NotificationChannels.Email,
                "Course completed",
                $"You've completed {body.CourseTitle}. Your certificate is available in My Learning.")
        ];
    }

    private static UserNotification Create(
        IntegrationEventDto envelope,
        string userId,
        string email,
        string channel,
        string title,
        string body) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventId = envelope.EventId,
            UserId = userId,
            UserEmail = email,
            Channel = channel,
            Title = title,
            Body = body,
            EventType = envelope.Type,
            Status = NotificationStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
