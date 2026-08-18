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
