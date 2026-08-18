namespace CampusHub.Enrollment.Api.Domain;

public static class EnrollmentStatus
{
    public const string Started = "Started";
    public const string SeatReserved = "SeatReserved";
    public const string PaymentPending = "PaymentPending";
    public const string Confirmed = "Confirmed";
    public const string Compensated = "Compensated";
    public const string Rejected = "Rejected";
}

public sealed class Enrollment
{
    public Guid Id { get; set; }
    public required string StudentId { get; set; }
    public required string StudentEmail { get; set; }
    public required string StudentName { get; set; }
    public Guid CourseId { get; set; }
    public required string CourseTitle { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = EnrollmentStatus.Started;
    public Guid? PaymentId { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
