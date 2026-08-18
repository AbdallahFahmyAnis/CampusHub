using System.Text.Json;
using CampusHub.Contracts.Events;
using CampusHub.Enrollment.Api.Domain;
using CampusHub.Enrollment.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using EnrollmentEntity = CampusHub.Enrollment.Api.Domain.Enrollment;

namespace CampusHub.Enrollment.Api.Sagas;

/// <summary>
/// Orchestrated enrollment/payment saga. Commands currently travel over HTTP
/// because this workspace has no Docker/RabbitMQ; saga states, compensation,
/// outbox, and idempotency are the same as they would be with MassTransit.
/// </summary>
public sealed class EnrollmentSaga(
    EnrollmentDbContext db,
    CatalogGateway catalog,
    PaymentGateway payments,
    ILogger<EnrollmentSaga> logger)
{
    public async Task<EnrollmentEntity> StartAsync(
        Guid courseId,
        string studentId,
        string studentEmail,
        string studentName,
        Guid tenantId,
        string accessToken,
        string simulateOutcome,
        CancellationToken ct)
    {
        var idempotencyKey = $"{studentId}:{courseId}";
        var existing = await db.Enrollments.SingleOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null && existing.Status is not EnrollmentStatus.Compensated and not EnrollmentStatus.Rejected)
        {
            return existing;
        }

        var course = await catalog.GetCourseAsync(courseId, accessToken, ct)
            ?? throw new InvalidOperationException("Course was not found.");

        if (!string.Equals(course.Status, "Published", StringComparison.OrdinalIgnoreCase) || !course.CanEnroll)
        {
            var rejected = existing ?? new EnrollmentEntity
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                StudentEmail = studentEmail,
                StudentName = studentName,
                CourseId = courseId,
                CourseTitle = course.Title,
                Amount = course.Price,
                Status = EnrollmentStatus.Rejected,
                FailureReason = "Course is full or not published.",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IdempotencyKey = idempotencyKey,
                TenantId = tenantId
            };
            rejected.Status = EnrollmentStatus.Rejected;
            rejected.FailureReason = "Course is full or not published.";
            rejected.UpdatedAt = DateTimeOffset.UtcNow;
            if (existing is null)
            {
                db.Enrollments.Add(rejected);
            }

            await db.SaveChangesAsync(ct);
            return rejected;
        }

        var enrollment = existing ?? new EnrollmentEntity
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            StudentEmail = studentEmail,
            StudentName = studentName,
            CourseId = courseId,
            CourseTitle = course.Title,
            Amount = course.Price,
            Status = EnrollmentStatus.Started,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IdempotencyKey = idempotencyKey,
            TenantId = tenantId
        };

        if (existing is null)
        {
            db.Enrollments.Add(enrollment);
        }
        else
        {
            enrollment.Status = EnrollmentStatus.Started;
            enrollment.FailureReason = null;
            enrollment.PaymentId = null;
            enrollment.UpdatedAt = DateTimeOffset.UtcNow;
            if (enrollment.TenantId == Guid.Empty)
            {
                enrollment.TenantId = tenantId;
            }
        }

        await db.SaveChangesAsync(ct);

        var reserved = await catalog.ReserveSeatAsync(courseId, ct);
        if (!reserved)
        {
            enrollment.Status = EnrollmentStatus.Rejected;
            enrollment.FailureReason = "No seats remaining.";
            enrollment.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return enrollment;
        }

        enrollment.Status = EnrollmentStatus.SeatReserved;
        enrollment.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var payment = await payments.InitiateAsync(
                enrollment.Id,
                courseId,
                studentId,
                course.Price,
                simulateOutcome,
                ct);
            enrollment.PaymentId = payment.PaymentId;
            enrollment.Status = EnrollmentStatus.PaymentPending;
            enrollment.UpdatedAt = DateTimeOffset.UtcNow;
            AddOutbox(EventTypes.EnrollmentStarted, new EnrollmentStartedV1(
                enrollment.Id,
                studentId,
                studentEmail,
                studentName,
                courseId,
                course.Title,
                course.Price));
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment initiation failed for {EnrollmentId}", enrollment.Id);
            await CompensateAsync(enrollment.Id, "Payment service failed.", ct);
        }

        return enrollment;
    }

    public async Task HandlePaymentSucceededAsync(Guid enrollmentId, Guid paymentId, CancellationToken ct)
    {
        var enrollment = await db.Enrollments.SingleOrDefaultAsync(e => e.Id == enrollmentId, ct);
        if (enrollment is null || enrollment.Status == EnrollmentStatus.Confirmed)
        {
            return;
        }

        enrollment.PaymentId = paymentId;
        enrollment.Status = EnrollmentStatus.Confirmed;
        enrollment.FailureReason = null;
        enrollment.UpdatedAt = DateTimeOffset.UtcNow;
        AddOutbox(EventTypes.EnrollmentConfirmed, new EnrollmentConfirmedV1(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.StudentEmail,
            enrollment.StudentName,
            enrollment.CourseId,
            enrollment.CourseTitle));
        await db.SaveChangesAsync(ct);
    }

    public async Task HandlePaymentFailedAsync(Guid enrollmentId, Guid paymentId, string reason, CancellationToken ct)
    {
        await CompensateAsync(enrollmentId, reason, ct, paymentId);
    }

    public async Task CompensateAsync(Guid enrollmentId, string reason, CancellationToken ct, Guid? paymentId = null)
    {
        var enrollment = await db.Enrollments.SingleOrDefaultAsync(e => e.Id == enrollmentId, ct);
        if (enrollment is null || enrollment.Status is EnrollmentStatus.Compensated or EnrollmentStatus.Rejected)
        {
            return;
        }

        if (enrollment.Status is EnrollmentStatus.SeatReserved or EnrollmentStatus.PaymentPending or EnrollmentStatus.Started)
        {
            await catalog.ReleaseSeatAsync(enrollment.CourseId, ct);
        }

        enrollment.PaymentId = paymentId ?? enrollment.PaymentId;
        enrollment.Status = EnrollmentStatus.Compensated;
        enrollment.FailureReason = reason;
        enrollment.UpdatedAt = DateTimeOffset.UtcNow;
        AddOutbox(EventTypes.EnrollmentCancelled, new EnrollmentCancelledV1(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.StudentEmail,
            enrollment.CourseId,
            enrollment.CourseTitle,
            reason));
        await db.SaveChangesAsync(ct);
    }

    private void AddOutbox(string type, object payload)
    {
        db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
