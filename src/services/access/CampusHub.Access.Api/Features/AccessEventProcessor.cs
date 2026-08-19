using System.Text.Json;
using CampusHub.Access.Api.Domain;
using CampusHub.Access.Api.Infrastructure;
using CampusHub.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Access.Api.Features;

public sealed class AccessEventProcessor(
    AccessDbContext db,
    CredentialSigner signer,
    ILogger<AccessEventProcessor> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task HandleAsync(IntegrationEventDto envelope, CancellationToken ct)
    {
        if (await db.Inbox.AnyAsync(x => x.EventId == envelope.EventId, ct))
        {
            return;
        }

        db.Inbox.Add(new InboxMessage
        {
            EventId = envelope.EventId,
            Type = envelope.Type,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        if (envelope.Type == EventTypes.EnrollmentConfirmed)
        {
            var body = JsonSerializer.Deserialize<EnrollmentConfirmedV1>(envelope.Payload, JsonOptions);
            if (body is not null)
            {
                await IssueAsync(body, ct);
            }
        }
        else if (envelope.Type == EventTypes.EnrollmentCancelled)
        {
            var body = JsonSerializer.Deserialize<EnrollmentCancelledV1>(envelope.Payload, JsonOptions);
            if (body is not null)
            {
                await RevokeAsync(body.EnrollmentId, ct);
            }
        }
        else if (envelope.Type == EventTypes.CourseCompleted)
        {
            var body = JsonSerializer.Deserialize<CourseCompletedV1>(envelope.Payload, JsonOptions);
            if (body is not null)
            {
                await IssueCertificateAsync(body, ct);
            }
        }
        else
        {
            logger.LogDebug("Access service ignored event {Type}", envelope.Type);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task IssueAsync(EnrollmentConfirmedV1 body, CancellationToken ct)
    {
        var existing = await db.Credentials.SingleOrDefaultAsync(c => c.EnrollmentId == body.EnrollmentId, ct);
        if (existing is not null)
        {
            existing.Status = CredentialStatus.Active;
            existing.StudentName = body.StudentName;
            existing.CourseTitle = body.CourseTitle;
            return;
        }

        var id = Guid.NewGuid();
        var expires = DateTimeOffset.UtcNow.AddYears(1);
        var token = signer.Sign(id, body.EnrollmentId, body.CourseId, CredentialKinds.CoursePass, expires.ToUnixTimeSeconds());
        db.Credentials.Add(new AccessCredential
        {
            Id = id,
            EnrollmentId = body.EnrollmentId,
            StudentId = body.StudentId,
            StudentName = body.StudentName,
            CourseId = body.CourseId,
            CourseTitle = body.CourseTitle,
            Kind = CredentialKinds.CoursePass,
            Token = token,
            Status = CredentialStatus.Active,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expires
        });
    }

    private async Task IssueCertificateAsync(CourseCompletedV1 body, CancellationToken ct)
    {
        // Idempotent: one certificate per enrollment
        var already = await db.Credentials.AnyAsync(
            c => c.EnrollmentId == body.EnrollmentId && c.Kind == CredentialKinds.Certificate, ct);
        if (already)
        {
            return;
        }

        var id = Guid.NewGuid();
        var expires = body.CompletedAt.AddYears(100); // certificates don't expire
        var token = signer.Sign(id, body.EnrollmentId, body.CourseId, CredentialKinds.Certificate, expires.ToUnixTimeSeconds());
        db.Credentials.Add(new AccessCredential
        {
            Id = id,
            EnrollmentId = body.EnrollmentId,
            StudentId = body.StudentId,
            StudentName = body.StudentName,
            CourseId = body.CourseId,
            CourseTitle = body.CourseTitle,
            Kind = CredentialKinds.Certificate,
            Token = token,
            Status = CredentialStatus.Active,
            IssuedAt = body.CompletedAt,
            ExpiresAt = expires
        });
        logger.LogInformation("Issued certificate for student {Student} on course {Course}", body.StudentId, body.CourseId);
    }

    private async Task RevokeAsync(Guid enrollmentId, CancellationToken ct)
    {
        var credential = await db.Credentials.SingleOrDefaultAsync(c => c.EnrollmentId == enrollmentId, ct);
        if (credential is not null)
        {
            credential.Status = CredentialStatus.Revoked;
        }
    }
}
