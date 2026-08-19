using System.Security.Cryptography;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Identity.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Identity.Api.Features;

public static class CampusEndpoints
{
    public static IEndpointRouteBuilder MapCampusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/identity/tenants/{tenantId:guid}/members", ListMembers).AllowAnonymous();
        app.MapPost("/api/identity/tenants/{tenantId:guid}/invites", CreateInvite).AllowAnonymous();
        app.MapGet("/api/identity/tenants/{tenantId:guid}/billing", GetBilling).AllowAnonymous();
        app.MapPost("/api/identity/tenants/{tenantId:guid}/billing/upgrade", UpgradeBilling).AllowAnonymous();
        app.MapGet("/api/identity/invites/{token}", GetInvite).AllowAnonymous();
        app.MapPost("/api/identity/invites/{token}/accept", AcceptInvite).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> ListMembers(
        Guid tenantId,
        HttpContext http,
        IConfiguration config,
        IdentityDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var tenant = await FindCampusAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return Results.NotFound(new { error = "Campus was not found." });
        }

        var members = await LoadMembersAsync(users, tenantId, ct);
        var invites = (await db.CampusInvites.AsNoTracking()
                .Where(i => i.TenantId == tenantId && i.AcceptedAt == null)
                .ToListAsync(ct))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
        var pending = invites
            .Where(i => i.ExpiresAt > DateTimeOffset.UtcNow)
            .Select(i => new PendingInviteDto(i.Email, i.DisplayName, i.Role, i.Token, i.ExpiresAt))
            .ToList();
        var seatsUsed = CountStudentSeats(members, pending);

        return Results.Ok(new CampusMembersDto(
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            Plans.SeatCap(tenant.Plan),
            seatsUsed,
            members,
            pending));
    }

    private static async Task<IResult> CreateInvite(
        Guid tenantId,
        CreateCampusInviteRequest request,
        HttpContext http,
        IConfiguration config,
        IdentityDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var tenant = await FindCampusAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return Results.NotFound(new { error = "Campus was not found." });
        }

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim();
        var role = request.Role?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName))
        {
            return Results.BadRequest(new { error = "Email and name are required." });
        }

        if (!Roles.All.Contains(role, StringComparer.Ordinal))
        {
            return Results.BadRequest(new { error = "Role must be Student, Teacher, or Administrator." });
        }

        if (await users.FindByEmailAsync(email) is not null)
        {
            return Results.Conflict(new { error = "A user with that email already exists." });
        }

        var members = await LoadMembersAsync(users, tenantId, ct);
        var pending = (await db.CampusInvites
                .Where(i => i.TenantId == tenantId && i.AcceptedAt == null)
                .ToListAsync(ct))
            .Where(i => i.ExpiresAt > DateTimeOffset.UtcNow)
            .ToList();
        if (role == Roles.Student && CountStudentSeats(members, pending.Select(ToPending)) >= Plans.SeatCap(tenant.Plan))
        {
            return Results.Conflict(new
            {
                error = $"This campus plan allows {Plans.SeatCap(tenant.Plan)} student seats. Upgrade to invite more students."
            });
        }

        var existing = pending.FirstOrDefault(i => string.Equals(i.Email, email, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.DisplayName = displayName;
            existing.Role = role;
            existing.Token = NewToken();
            existing.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new CreatedInviteDto(existing.Token, existing.Email, existing.Role, existing.ExpiresAt));
        }

        var invite = new CampusInvite
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            DisplayName = displayName,
            Role = role,
            Token = NewToken(),
            CreatedBy = request.CreatedBy?.Trim() ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.CampusInvites.Add(invite);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/identity/invites/{invite.Token}",
            new CreatedInviteDto(invite.Token, invite.Email, invite.Role, invite.ExpiresAt));
    }

    private static async Task<IResult> GetBilling(
        Guid tenantId,
        HttpContext http,
        IConfiguration config,
        IdentityDbContext db,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var tenant = await FindCampusAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return Results.NotFound(new { error = "Campus was not found." });
        }

        return Results.Ok(ToBillingDto(tenant));
    }

    private static async Task<IResult> UpgradeBilling(
        Guid tenantId,
        HttpContext http,
        IConfiguration config,
        IdentityDbContext db,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var tenant = await FindCampusTrackedAsync(db, tenantId, ct);
        if (tenant is null)
        {
            return Results.NotFound(new { error = "Campus was not found." });
        }

        var next = Plans.NextPlan(tenant.Plan);
        if (next is null)
        {
            return Results.Conflict(new { error = "This campus is already on the highest plan." });
        }

        tenant.Plan = next;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            upgraded = true,
            plan = tenant.Plan,
            billing = ToBillingDto(tenant),
            message = "Plan upgraded. Sign in again so Ask AI, chat, and seat limits pick up the new plan."
        });
    }

    private static CampusBillingDto ToBillingDto(Tenant tenant)
    {
        var next = Plans.NextPlan(tenant.Plan);
        return new CampusBillingDto(
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            Plans.SeatCap(tenant.Plan),
            Plans.MonthlyPrice(tenant.Plan),
            Plans.AllowsModelAi(tenant.Plan),
            Plans.AllowsChat(tenant.Plan),
            next,
            next is null ? null : Plans.MonthlyPrice(next),
            [
                new PlanOptionDto(Plans.Free, "Free", Plans.MonthlyPrice(Plans.Free), Plans.SeatCap(Plans.Free), false, false),
                new PlanOptionDto(Plans.Campus, "Campus", Plans.MonthlyPrice(Plans.Campus), Plans.SeatCap(Plans.Campus), true, true),
                new PlanOptionDto(Plans.Enterprise, "Enterprise", Plans.MonthlyPrice(Plans.Enterprise), Plans.SeatCap(Plans.Enterprise), true, true)
            ]);
    }

    private static async Task<IResult> GetInvite(
        string token,
        HttpContext http,
        IConfiguration config,
        IdentityDbContext db,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var invite = await FindOpenInviteAsync(db, token, ct);
        if (invite is null)
        {
            return Results.NotFound(new { error = "This invite is invalid or has expired." });
        }

        var tenant = await FindCampusAsync(db, invite.TenantId, ct);
        return Results.Ok(new OpenInviteDto(
            invite.Email,
            invite.DisplayName,
            invite.Role,
            tenant?.Name ?? SeedTenants.DefaultName,
            invite.ExpiresAt));
    }

    private static async Task<IResult> AcceptInvite(
        string token,
        AcceptCampusInviteRequest request,
        HttpContext http,
        IConfiguration config,
        IdentityDbContext db,
        UserManager<ApplicationUser> users,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var invite = await FindOpenInviteAsync(db, token, ct);
        if (invite is null)
        {
            return Results.NotFound(new { error = "This invite is invalid or has expired." });
        }

        var password = request.Password ?? string.Empty;
        if (password.Length < 10)
        {
            return Results.BadRequest(new { error = "Password must be at least 10 characters." });
        }

        if (await users.FindByEmailAsync(invite.Email) is not null)
        {
            return Results.Conflict(new { error = "A user with that email already exists." });
        }

        var tenant = await FindCampusAsync(db, invite.TenantId, ct);
        if (tenant is not null && invite.Role == Roles.Student)
        {
            var members = await LoadMembersAsync(users, invite.TenantId, ct);
            var pending = (await db.CampusInvites.AsNoTracking()
                    .Where(i => i.TenantId == invite.TenantId && i.AcceptedAt == null && i.Token != invite.Token)
                    .ToListAsync(ct))
                .Where(i => i.ExpiresAt > DateTimeOffset.UtcNow)
                .Select(i => new PendingInviteDto(i.Email, i.DisplayName, i.Role, i.Token, i.ExpiresAt))
                .ToList();
            if (CountStudentSeats(members, pending) >= Plans.SeatCap(tenant.Plan))
            {
                return Results.Conflict(new { error = "This campus has no student seats left." });
            }
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? invite.DisplayName : request.DisplayName.Trim();
        var user = new ApplicationUser
        {
            UserName = invite.Email,
            Email = invite.Email,
            EmailConfirmed = true,
            DisplayName = displayName,
            TenantId = invite.TenantId
        };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return Results.BadRequest(new { error = string.Join(" ", created.Errors.Select(e => e.Description)) });
        }

        await users.AddToRoleAsync(user, invite.Role);
        invite.AcceptedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { email = user.Email, tenantId = invite.TenantId });
    }

    private static async Task<Tenant?> FindCampusTrackedAsync(IdentityDbContext db, Guid tenantId, CancellationToken ct)
    {
        var tenants = await db.Tenants.ToListAsync(ct);
        return tenants.FirstOrDefault(t => t.Id == tenantId)
               ?? tenants.FirstOrDefault(t => tenantId == Tenancy.DefaultTenantId && t.Slug == SeedTenants.DefaultSlug);
    }

    private static async Task<Tenant?> FindCampusAsync(IdentityDbContext db, Guid tenantId, CancellationToken ct)
    {
        var tenants = await db.Tenants.AsNoTracking().ToListAsync(ct);
        var match = tenants.FirstOrDefault(t => t.Id == tenantId)
                    ?? tenants.FirstOrDefault(t => tenantId == Tenancy.DefaultTenantId && t.Slug == SeedTenants.DefaultSlug);
        if (match is not null)
        {
            return match;
        }

        return tenantId == Tenancy.DefaultTenantId
            ? new Tenant
            {
                Id = Tenancy.DefaultTenantId,
                Name = SeedTenants.DefaultName,
                Slug = SeedTenants.DefaultSlug,
                Plan = SeedTenants.DefaultPlan,
                CreatedAt = DateTimeOffset.UtcNow
            }
            : null;
    }

    private static async Task<CampusInvite?> FindOpenInviteAsync(IdentityDbContext db, string token, CancellationToken ct)
    {
        var invite = await db.CampusInvites.SingleOrDefaultAsync(i => i.Token == token, ct);
        if (invite is null || invite.AcceptedAt is not null || invite.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return invite;
    }

    private static async Task<List<CampusMemberDto>> LoadMembersAsync(
        UserManager<ApplicationUser> users,
        Guid tenantId,
        CancellationToken ct)
    {
        var people = (await users.Users.AsNoTracking().ToListAsync(ct))
            .Where(u => u.TenantId == tenantId || (tenantId == Tenancy.DefaultTenantId && u.TenantId == Guid.Empty))
            .OrderBy(u => u.DisplayName)
            .ToList();
        var result = new List<CampusMemberDto>(people.Count);
        foreach (var user in people)
        {
            result.Add(new CampusMemberDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                [.. await users.GetRolesAsync(user)]));
        }

        return result;
    }

    private static int CountStudentSeats(IEnumerable<CampusMemberDto> members, IEnumerable<PendingInviteDto> pending) =>
        members.Count(m => m.Roles.Contains(Roles.Student, StringComparer.Ordinal))
        + pending.Count(i => i.Role == Roles.Student);

    private static PendingInviteDto ToPending(CampusInvite invite) =>
        new(invite.Email, invite.DisplayName, invite.Role, invite.Token, invite.ExpiresAt);

    private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private static bool IsInternal(HttpContext http, IConfiguration config)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) &&
               string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }
}

public sealed record CampusMembersDto(
    Guid TenantId,
    string TenantName,
    string Plan,
    int SeatCap,
    int SeatsUsed,
    IReadOnlyList<CampusMemberDto> Members,
    IReadOnlyList<PendingInviteDto> Invites);

public sealed record CampusMemberDto(string Id, string Email, string DisplayName, string[] Roles);

public sealed record PendingInviteDto(string Email, string DisplayName, string Role, string Token, DateTimeOffset ExpiresAt);

public sealed record CreatedInviteDto(string Token, string Email, string Role, DateTimeOffset ExpiresAt);

public sealed record OpenInviteDto(string Email, string DisplayName, string Role, string CampusName, DateTimeOffset ExpiresAt);

public sealed record CreateCampusInviteRequest(string Email, string DisplayName, string Role, string? CreatedBy);

public sealed record AcceptCampusInviteRequest(string Password, string? DisplayName);

public sealed record CampusBillingDto(
    Guid TenantId,
    string TenantName,
    string Plan,
    int SeatCap,
    decimal MonthlyPrice,
    bool AllowsModelAi,
    bool AllowsChat,
    string? NextPlan,
    decimal? NextPlanPrice,
    IReadOnlyList<PlanOptionDto> Options);

public sealed record PlanOptionDto(
    string Id,
    string Name,
    decimal MonthlyPrice,
    int SeatCap,
    bool AllowsModelAi,
    bool AllowsChat);
