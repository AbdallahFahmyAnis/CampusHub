namespace CampusHub.Payment.Api.Domain;

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}

public sealed class PaymentIntent
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }
    public required string StudentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = PaymentStatus.Pending;
    public string SimulateOutcome { get; set; } = "Succeeded";
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
