using CampusHub.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Enrollment.Api.Infrastructure;

internal static class EnrollmentSchema
{
    public static async Task EnsureAsync(EnrollmentDbContext db, CancellationToken ct)
    {
        await db.Database.EnsureCreatedAsync(ct);
        await TryAsync(db, "ALTER TABLE Enrollments ADD COLUMN TenantId TEXT", ct);
        await TryAsync(db, $"""
            UPDATE Enrollments SET TenantId = '{SeedTenants.DefaultId}'
            WHERE TenantId IS NULL OR TenantId = '' OR TenantId = '00000000-0000-0000-0000-000000000000';
            """, ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS CourseWaitlists (
                Id TEXT NOT NULL CONSTRAINT PK_CourseWaitlists PRIMARY KEY,
                TenantId TEXT NOT NULL,
                CourseId TEXT NOT NULL,
                CourseTitle TEXT NOT NULL,
                StudentId TEXT NOT NULL,
                StudentEmail TEXT NOT NULL,
                StudentName TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """, ct);
        await TryAsync(db, """
            CREATE UNIQUE INDEX IF NOT EXISTS IX_CourseWaitlists_CourseId_StudentId
            ON CourseWaitlists (CourseId, StudentId);
            """, ct);
    }

    private static async Task TryAsync(EnrollmentDbContext db, string sql, CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, ct);
        }
        catch
        {
            // Column already exists on an older enrollment.db.
        }
    }
}
