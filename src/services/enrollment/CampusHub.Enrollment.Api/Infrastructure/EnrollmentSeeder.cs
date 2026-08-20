using CampusHub.BuildingBlocks.Security;
using CampusHub.Enrollment.Api.Domain;
using Microsoft.EntityFrameworkCore;
using EnrollmentEntity = CampusHub.Enrollment.Api.Domain.Enrollment;

namespace CampusHub.Enrollment.Api.Infrastructure;

/// <summary>SDD CH-S23 / CH-S24 — demo waitlist and confirmed enrollments for roster.</summary>
internal static class EnrollmentSeeder
{
    private static readonly Guid AlgorithmsId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly Guid LinearId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    private static readonly Guid DistributedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");

    public static async Task SeedDemoAsync(EnrollmentDbContext db, CancellationToken ct)
    {
        await SeedWaitlistDemoAsync(db, ct);
        await SeedConfirmedEnrollmentsAsync(db, ct);
    }

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

    public static async Task SeedConfirmedEnrollmentsAsync(EnrollmentDbContext db, CancellationToken ct)
    {
        try
        {
            if (await db.Enrollments.AnyAsync(e => e.Status == EnrollmentStatus.Confirmed, ct))
            {
                return;
            }
        }
        catch
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.Enrollments.AddRange(
            Confirmed(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01"),
                AlgorithmsId,
                "Introduction to Algorithms",
                SeedUsers.StudentId,
                SeedUsers.StudentEmail,
                "Sam Student",
                49.99m,
                now.AddDays(-14)),
            Confirmed(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd02"),
                AlgorithmsId,
                "Introduction to Algorithms",
                "reviewer-noah",
                "noah@campushub.local",
                "Noah Okonkwo",
                49.99m,
                now.AddDays(-10)),
            Confirmed(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd03"),
                LinearId,
                "Linear Algebra",
                SeedUsers.StudentId,
                SeedUsers.StudentEmail,
                "Sam Student",
                39.99m,
                now.AddDays(-7)));
        await db.SaveChangesAsync(ct);
    }

    private static EnrollmentEntity Confirmed(
        Guid id,
        Guid courseId,
        string courseTitle,
        string studentId,
        string email,
        string name,
        decimal amount,
        DateTimeOffset enrolledAt) =>
        new()
        {
            Id = id,
            TenantId = Tenancy.DefaultTenantId,
            CourseId = courseId,
            CourseTitle = courseTitle,
            StudentId = studentId,
            StudentEmail = email,
            StudentName = name,
            Amount = amount,
            Status = EnrollmentStatus.Confirmed,
            CreatedAt = enrolledAt,
            UpdatedAt = enrolledAt,
            IdempotencyKey = $"seed-{id:N}",
        };
}
