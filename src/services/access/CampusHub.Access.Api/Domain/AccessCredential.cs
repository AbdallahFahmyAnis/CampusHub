namespace CampusHub.Access.Api.Domain;

public sealed class AccessCredential
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Kind { get; set; } = CredentialKinds.CoursePass;
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = CredentialStatus.Active;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public static class CredentialKinds
{
    public const string CoursePass = "CoursePass";
    public const string Certificate = "Certificate";
}

public static class CredentialStatus
{
    public const string Active = "Active";
    public const string Revoked = "Revoked";
    public const string Expired = "Expired";
}
