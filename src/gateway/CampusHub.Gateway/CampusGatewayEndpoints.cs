using System.Security.Claims;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Gateway.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CampusHub.Gateway;

public static class CampusGatewayEndpoints
{
    public static WebApplication MapCampusGatewayEndpoints(this WebApplication app)
    {
        var campus = app.MapGroup("/api/campus").RequireAuthorization().DisableAntiforgery();
        campus.MapGet("/members", ListMembers);
        campus.MapPost("/invites", CreateInvite);
        campus.MapGet("/billing", GetBilling);
        campus.MapPost("/billing/upgrade", UpgradeBilling);
        campus.MapGet("/dashboard", GetDashboard);

        app.MapGet("/api/invites/{token}", GetInvite).AllowAnonymous();
        app.MapPost("/api/invites/{token}/accept", AcceptInvite).AllowAnonymous().DisableAntiforgery();
        return app;
    }

    private static async Task<IResult> ListMembers(ClaimsPrincipal user, DownstreamApi api, CancellationToken ct)
    {
        if (!user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        var tenantId = Tenancy.TenantId(user);
        var members = await api.GetInternalAsync<CampusMembersDto>(
            "identity",
            $"/api/identity/tenants/{tenantId}/members",
            ct);
        return members is null
            ? Results.NotFound(new { error = "Campus was not found." })
            : Results.Ok(members);
    }

    private static async Task<IResult> CreateInvite(
        [FromBody] CreateCampusInviteBody request,
        ClaimsPrincipal user,
        HttpContext http,
        DownstreamApi api,
        CancellationToken ct)
    {
        if (!user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        var tenantId = Tenancy.TenantId(user);
        var createdBy = user.FindFirstValue("email") ?? user.Identity?.Name ?? string.Empty;
        var (ok, error, created) = await api.PostJsonResultAsync<CreateCampusInviteBody, CreatedInviteDto>(
            "identity",
            $"/api/identity/tenants/{tenantId}/invites",
            new CreateCampusInviteBody(request.Email, request.DisplayName, request.Role, createdBy),
            ct,
            internalKey: true);
        if (!ok || created is null)
        {
            return Results.BadRequest(new { error = error ?? "Could not create the invite." });
        }

        var origin = $"{http.Request.Scheme}://{http.Request.Host}";
        return Results.Ok(new
        {
            created.Token,
            created.Email,
            created.Role,
            created.ExpiresAt,
            inviteUrl = $"{origin}/invite/{created.Token}"
        });
    }

    private static async Task<IResult> GetInvite(string token, DownstreamApi api, CancellationToken ct)
    {
        var invite = await api.GetInternalAsync<OpenInviteDto>(
            "identity",
            $"/api/identity/invites/{Uri.EscapeDataString(token)}",
            ct);
        return invite is null
            ? Results.NotFound(new { error = "This invite is invalid or has expired." })
            : Results.Ok(invite);
    }

    private static async Task<IResult> AcceptInvite(
        string token,
        [FromBody] AcceptCampusInviteBody request,
        DownstreamApi api,
        CancellationToken ct)
    {
        var (ok, error) = await api.PostJsonAsync(
            "identity",
            $"/api/identity/invites/{Uri.EscapeDataString(token)}/accept",
            request,
            ct,
            internalKey: true);
        return ok
            ? Results.Ok(new { accepted = true })
            : Results.BadRequest(new { error = error ?? "Could not accept the invite." });
    }

    private static async Task<IResult> GetDashboard(
        ClaimsPrincipal user,
        DownstreamApi api,
        CancellationToken ct)
    {
        if (!user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        var tenantId = Tenancy.TenantId(user);

        var membersTask = api.GetInternalAsync<CampusMembersDto>("identity", $"/api/identity/tenants/{tenantId}/members", ct);
        var billingTask = api.GetInternalAsync<CampusBillingDto>("identity", $"/api/identity/tenants/{tenantId}/billing", ct);

        await Task.WhenAll(membersTask, billingTask);

        var members = await membersTask;
        var billing = await billingTask;

        var isPlatform = tenantId == Tenancy.DefaultTenantId;
        var memberCount = members is null ? 0 : members.Members.Length;
        var studentSeats = members is null ? 0 : members.SeatsUsed;
        var seatCap = billing is null ? 0 : billing.SeatCap;
        var pendingInvites = members is null ? 0 : members.Invites.Length;
        var allowsModelAi = billing is not null && billing.AllowsModelAi;
        var allowsChat = billing is not null && billing.AllowsChat;
        var monthlyPrice = billing is null ? 0m : billing.MonthlyPrice;
        var nextPlan = billing?.NextPlan;

        return Results.Ok(new
        {
            tenantId,
            tenantName = Tenancy.TenantName(user),
            plan = Tenancy.Plan(user),
            isPlatformAdmin = isPlatform,
            memberCount,
            studentSeats,
            seatCap,
            pendingInvites,
            allowsModelAi,
            allowsChat,
            monthlyPrice,
            nextPlan
        });
    }

    private static async Task<IResult> GetBilling(ClaimsPrincipal user, DownstreamApi api, CancellationToken ct)
    {
        if (!user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        var tenantId = Tenancy.TenantId(user);
        var billing = await api.GetInternalAsync<CampusBillingDto>(
            "identity",
            $"/api/identity/tenants/{tenantId}/billing",
            ct);
        return billing is null
            ? Results.NotFound(new { error = "Campus was not found." })
            : Results.Ok(billing);
    }

    private static async Task<IResult> UpgradeBilling(ClaimsPrincipal user, DownstreamApi api, CancellationToken ct)
    {
        if (!user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        var tenantId = Tenancy.TenantId(user);
        var (ok, error, body) = await api.PostJsonResultAsync<object, UpgradeBillingResponse>(
            "identity",
            $"/api/identity/tenants/{tenantId}/billing/upgrade",
            new { },
            ct,
            internalKey: true);
        if (!ok || body is null)
        {
            return Results.BadRequest(new { error = error ?? "Could not upgrade the plan." });
        }

        return Results.Ok(new
        {
            body.Upgraded,
            body.Plan,
            body.Message,
            billing = body.Billing
        });
    }

    private sealed record CampusMembersDto(
        Guid TenantId,
        string TenantName,
        string Plan,
        int SeatCap,
        int SeatsUsed,
        CampusMemberDto[] Members,
        PendingInviteDto[] Invites);

    private sealed record CampusMemberDto(string Id, string Email, string DisplayName, string[] Roles);

    private sealed record PendingInviteDto(string Email, string DisplayName, string Role, string Token, DateTimeOffset ExpiresAt);

    private sealed record CreatedInviteDto(string Token, string Email, string Role, DateTimeOffset ExpiresAt);

    private sealed record OpenInviteDto(string Email, string DisplayName, string Role, string CampusName, DateTimeOffset ExpiresAt);

    private sealed record CreateCampusInviteBody(string Email, string DisplayName, string Role, string? CreatedBy);

    private sealed record AcceptCampusInviteBody(string Password, string? DisplayName);

    private sealed record CampusBillingDto(
        Guid TenantId,
        string TenantName,
        string Plan,
        int SeatCap,
        decimal MonthlyPrice,
        bool AllowsModelAi,
        bool AllowsChat,
        string? NextPlan,
        decimal? NextPlanPrice,
        PlanOptionDto[] Options);

    private sealed record PlanOptionDto(
        string Id,
        string Name,
        decimal MonthlyPrice,
        int SeatCap,
        bool AllowsModelAi,
        bool AllowsChat);

    private sealed record UpgradeBillingResponse(
        bool Upgraded,
        string Plan,
        string Message,
        CampusBillingDto? Billing);
}
