using System.Security.Claims;

namespace CampusHub.BuildingBlocks.Security;

public static class Roles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Administrator = "Administrator";

    public static readonly string[] All = [Student, Teacher, Administrator];
}

public static class Permissions
{
    public const string CatalogRead = "catalog.read";
    public const string CatalogManage = "catalog.manage";
    public const string EnrollmentStart = "enrollment.start";
    public const string EnrollmentRead = "enrollment.read";
    public const string PaymentsRead = "payments.read";
    public const string AdminAccess = "admin.access";
}

public static class Scopes
{
    public const string CatalogApi = "catalog.api";
    public const string EnrollmentApi = "enrollment.api";
    public const string PaymentApi = "payment.api";
    public const string NotificationApi = "notification.api";
    public const string AccessApi = "access.api";
    public const string ChatApi = "chat.api";
}

public static class Clients
{
    public const string Gateway = "campushub-gateway";
    public const string EnrollmentService = "campushub-enrollment";
    public const string CatalogService = "campushub-catalog";
}

public static class SeedUsers
{
    public const string AdminId = "11111111-1111-1111-1111-111111111111";
    public const string TeacherId = "22222222-2222-2222-2222-222222222222";
    public const string StudentId = "33333333-3333-3333-3333-333333333333";

    public const string AdminEmail = "admin@campushub.local";
    public const string TeacherEmail = "teacher@campushub.local";
    public const string StudentEmail = "student@campushub.local";
}

public static class SeedTenants
{
    public const string DefaultId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public const string DefaultSlug = "campushub";
    public const string DefaultName = "CampusHub Demo";
    public const string DefaultPlan = Plans.Campus;
}

public static class Plans
{
    public const string Free = "free";
    public const string Campus = "campus";
    public const string Enterprise = "enterprise";

    public static int SeatCap(string plan) =>
        plan.ToLowerInvariant() switch
        {
            Enterprise => int.MaxValue,
            Campus => 500,
            _ => 25
        };

    public static bool AllowsModelAi(string plan) =>
        !string.Equals(plan, Free, StringComparison.OrdinalIgnoreCase);

    public static bool AllowsChat(string plan) =>
        !string.Equals(plan, Free, StringComparison.OrdinalIgnoreCase);

    public static decimal MonthlyPrice(string plan) =>
        plan.ToLowerInvariant() switch
        {
            Enterprise => 199m,
            Campus => 49m,
            _ => 0m
        };

    public static string? NextPlan(string plan) =>
        plan.ToLowerInvariant() switch
        {
            Free => Campus,
            Campus => Enterprise,
            _ => null
        };
}

public static class Tenancy
{
    public const string TenantIdClaim = "tenant_id";
    public const string TenantNameClaim = "tenant_name";
    public const string PlanClaim = "plan";

    public static Guid DefaultTenantId => Guid.Parse(SeedTenants.DefaultId);

    public static Guid TenantId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(TenantIdClaim);
        return Guid.TryParse(raw, out var id) ? id : DefaultTenantId;
    }

    public static string Plan(ClaimsPrincipal user) =>
        user.FindFirstValue(PlanClaim) is { Length: > 0 } plan ? plan : SeedTenants.DefaultPlan;

    public static string TenantName(ClaimsPrincipal user) =>
        user.FindFirstValue(TenantNameClaim) ?? SeedTenants.DefaultName;
}

