using CampusHub.BuildingBlocks.Security;
using CampusHub.Notification.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Notification.Api.Infrastructure;

public static class NotificationSeeder
{
    public static async Task SeedAsync(NotificationDbContext db, CancellationToken ct)
    {
        if (await db.Notifications.AnyAsync(ct))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.Notifications.AddRange(
            Note(SeedUsers.StudentId, SeedUsers.StudentEmail, "Welcome to CampusHub",
                "Your student catalog is live. Preview a lecture, then enroll to unlock the full course, Q&A, and a signed pass.", now.AddHours(-6)),
            Note(SeedUsers.StudentId, SeedUsers.StudentEmail, "Course pass reminder",
                "After a confirmed enrollment, open Pass in the top bar to show your QR at the door.", now.AddHours(-2)),
            Note(SeedUsers.TeacherId, SeedUsers.TeacherEmail, "Teaching on CampusHub",
                "Create or edit a course from My courses. Students only see it after you publish.", now.AddHours(-5)),
            Note(SeedUsers.AdminId, SeedUsers.AdminEmail, "Ops console is ready",
                "Register students and teachers, add categories, and review enrollments at /ops.", now.AddHours(-4)));

        await db.SaveChangesAsync(ct);
    }

    private static UserNotification Note(string userId, string email, string title, string body, DateTimeOffset created) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserId = userId,
            UserEmail = email,
            Channel = NotificationChannels.InApp,
            Title = title,
            Body = body,
            EventType = "seed.welcome",
            Status = NotificationStatus.Sent,
            CreatedAt = created
        };
}
