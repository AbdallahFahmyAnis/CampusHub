namespace CampusHub.Contracts.Events;

public static class EventTypes
{
    public const string EnrollmentStarted = "edu.enrollment.started.v1";
    public const string EnrollmentConfirmed = "edu.enrollment.confirmed.v1";
    public const string EnrollmentCancelled = "edu.enrollment.cancelled.v1";
    public const string PaymentSucceeded = "edu.payment.succeeded.v1";
    public const string PaymentFailed = "edu.payment.failed.v1";
}

public sealed record EnrollmentStartedV1(Guid EnrollmentId, string StudentId, string StudentEmail, string StudentName, Guid CourseId, string CourseTitle, decimal Amount);
public sealed record EnrollmentConfirmedV1(Guid EnrollmentId, string StudentId, string StudentEmail, string StudentName, Guid CourseId, string CourseTitle);
public sealed record EnrollmentCancelledV1(Guid EnrollmentId, string StudentId, string StudentEmail, Guid CourseId, string CourseTitle, string Reason);
public sealed record PaymentSucceededV1(Guid PaymentId, Guid EnrollmentId);
public sealed record PaymentFailedV1(Guid PaymentId, Guid EnrollmentId, string Reason);

/// <summary>
/// Transport envelope used until RabbitMQ is available. Payload is the serialized event body.
/// </summary>
public sealed record IntegrationEventDto(Guid EventId, string Type, DateTimeOffset OccurredAt, string Payload);
