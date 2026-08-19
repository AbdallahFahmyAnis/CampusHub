using CampusHub.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Identity.Api.Data;

internal static class IdentitySchema
{
    public static async Task EnsureAsync(IdentityDbContext db, CancellationToken ct)
    {
        await db.Database.EnsureCreatedAsync(ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS Tenants (
                Id TEXT NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY,
                Name TEXT NOT NULL,
                Slug TEXT NOT NULL,
                Plan TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """, ct);
        await TryAsync(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Tenants_Slug ON Tenants (Slug);", ct);
        await TryAsync(db, "ALTER TABLE AspNetUsers ADD COLUMN TenantId TEXT", ct);
        await TryAsync(db, $"""
            UPDATE AspNetUsers SET TenantId = '{SeedTenants.DefaultId}'
            WHERE TenantId IS NULL OR TenantId = '' OR TenantId = '00000000-0000-0000-0000-000000000000';
            """, ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS CampusInvites (
                Id TEXT NOT NULL CONSTRAINT PK_CampusInvites PRIMARY KEY,
                TenantId TEXT NOT NULL,
                Email TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Role TEXT NOT NULL,
                Token TEXT NOT NULL,
                CreatedBy TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                ExpiresAt TEXT NOT NULL,
                AcceptedAt TEXT NULL
            );
            """, ct);
        await TryAsync(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_CampusInvites_Token ON CampusInvites (Token);", ct);
        await TryAsync(db, $"""
            UPDATE Tenants SET Id = '{SeedTenants.DefaultId}'
            WHERE Slug = '{SeedTenants.DefaultSlug}';
            """, ct);
    }

    private static async Task TryAsync(IdentityDbContext db, string sql, CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, ct);
        }
        catch
        {
            // Column or table already exists on an older identity.db.
        }
    }
}
