namespace CampusHub.Access.Api.Domain;

public sealed class AttendanceScan
{
    public Guid Id { get; set; }
    public Guid CredentialId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string ScannedBy { get; set; } = string.Empty;
    public DateTimeOffset ScannedAt { get; set; }
}
