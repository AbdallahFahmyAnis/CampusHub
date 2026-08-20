using CampusHub.BuildingBlocks.Security;
using CampusHub.Enrollment.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Enrollment.Api.Infrastructure;

/// <summary>SDD CH-S23 — specs/023-course-waitlist. Demo waitlist rows for full Distributed course.</summary>
internal static class EnrollmentSeeder
{
    private static readonly Guid DistributedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");

    public static async Task SeedWaitlistDemoAsync(EnrollmentDbContext db, CancellationToken ct)
    {
        try
        {
            if (await db.CourseWaitlists.AnyAsync(ct))
            {
                return;
            }
        }
        catch
        {
            return;
        }

        // Placeholder ahead of the demo student so position is #2 when they join from UI;
        // seed the student already waitlisted for a clickable My enrollments list.
        db.CourseWaitlists.AddRange(
            new CourseWaitlist
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01"),
                TenantId = Tenancy.DefaultTenantId,
                CourseId = DistributedId,
                CourseTitle = "Distributed Systems Studio",
                StudentId = "44444444-4444-4444-4444-444444444401",
                StudentEmail = "waitlist-peer@campushub.local",
                StudentName = "Waitlist Peer",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            },
            new CourseWaitlist
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02"),
                TenantId = Tenancy.DefaultTenantId,
                CourseId = DistributedId,
                CourseTitle = "Distributed Systems Studio",
                StudentId = SeedUsers.StudentId,
                StudentEmail = SeedUsers.StudentEmail,
                StudentName = "Sam Student",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            });
        await db.SaveChangesAsync(ct);
    }
}
