using System.Net.Http.Json;
using CampusHub.Payment.Api.Domain;
using CampusHub.Payment.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Payment.Api.Features;

/// <summary>SDD CH-S19 / specs/019-enroll-checkout. Internal mock PSP — not exposed on the gateway.</summary>
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/intents", Create).AllowAnonymous();
        app.MapGet("/api/payments/{id:guid}", Get).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> Create(
        CreatePaymentRequest request,
        HttpContext http,
        IConfiguration config,
        PaymentDbContext db,
        CancellationToken ct)
    {
        if (!IsInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var existing = await db.Payments.SingleOrDefaultAsync(p => p.EnrollmentId == request.EnrollmentId, ct);
        if (existing is not null)
        {
            return Results.Ok(new PaymentDto(existing.Id, existing.EnrollmentId, existing.Status));
        }

        var payment = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            EnrollmentId = request.EnrollmentId,
            CourseId = request.CourseId,
            StudentId = request.StudentId,
            Amount = request.Amount,
            SimulateOutcome = string.IsNullOrWhiteSpace(request.SimulateOutcome) ? "Succeeded" : request.SimulateOutcome,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return Results.Accepted($"/api/payments/{payment.Id}", new PaymentDto(payment.Id, payment.EnrollmentId, payment.Status));
    }

    private static async Task<IResult> Get(Guid id, PaymentDbContext db, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
        return payment is null ? Results.NotFound() : Results.Ok(new PaymentDto(payment.Id, payment.EnrollmentId, payment.Status));
    }

    private static bool IsInternal(HttpContext http, IConfiguration config)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        return http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) &&
               string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }
}

public sealed record CreatePaymentRequest(Guid EnrollmentId, Guid CourseId, string StudentId, decimal Amount, string? SimulateOutcome);
public sealed record PaymentDto(Guid PaymentId, Guid EnrollmentId, string Status);
