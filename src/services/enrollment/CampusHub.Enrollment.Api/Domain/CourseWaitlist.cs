namespace CampusHub.Enrollment.Api.Domain;

/// <summary>SDD CH-S23 — specs/023-course-waitlist. Student queue for a full course.</summary>
public sealed class CourseWaitlist
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CourseId { get; set; }
    public required string CourseTitle { get; set; }
    public required string StudentId { get; set; }
    public required string StudentEmail { get; set; }
    public required string StudentName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
