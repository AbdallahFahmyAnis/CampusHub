using System.Security.Claims;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

public static class AssignmentEndpoints
{
    public static RouteGroupBuilder MapAssignmentEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/assignments", ListAssignments);
        api.MapPost("/courses/{id:guid}/assignments", CreateAssignment).RequireAuthorization("CanManageCatalog");
        api.MapPost("/courses/{id:guid}/assignments/{assignmentId:guid}/submit", SubmitAssignment);
        api.MapGet("/courses/{id:guid}/assignments/{assignmentId:guid}/submissions", ListSubmissions)
            .RequireAuthorization("CanManageCatalog");
        api.MapPost("/courses/{id:guid}/assignments/{assignmentId:guid}/submissions/{submissionId:guid}/grade", GradeSubmission)
            .RequireAuthorization("CanManageCatalog");
        return api;
    }

    private static async Task<IResult> ListAssignments(
        Guid id,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        var staff = CatalogEndpoints.CanManage(user) || CatalogEndpoints.IsOwner(course, user);

        List<CourseAssignment> assignments;
        try
        {
            assignments = await db.CourseAssignments.AsNoTracking()
                .Where(a => a.CourseId == id)
                .ToListAsync(ct);
        }
        catch
        {
            return Results.Ok(Array.Empty<AssignmentSummaryDto>());
        }

        assignments = [.. assignments.OrderBy(a => a.CreatedAt)];

        List<CourseAssignmentSubmission> submissions;
        try
        {
            submissions = await db.CourseAssignmentSubmissions.AsNoTracking()
                .Where(s => s.CourseId == id)
                .ToListAsync(ct);
        }
        catch
        {
            submissions = [];
        }

        var result = assignments.Select(a =>
        {
            var mine = submissions.FirstOrDefault(s => s.AssignmentId == a.Id && s.StudentId == studentId);
            var count = submissions.Count(s => s.AssignmentId == a.Id);
            return new AssignmentSummaryDto(
                a.Id,
                a.Title,
                a.Instructions,
                a.MaxScore,
                mine is not null,
                mine?.Score,
                mine?.Feedback,
                staff ? count : 0);
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> CreateAssignment(
        Guid id,
        CreateAssignmentRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        if (!CatalogEndpoints.IsOwner(course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Instructions))
        {
            return Results.BadRequest(new { error = "Title and instructions are required." });
        }

        var assignment = new CourseAssignment
        {
            Id = Guid.NewGuid(),
            CourseId = id,
            Title = request.Title.Trim(),
            Instructions = request.Instructions.Trim(),
            MaxScore = request.MaxScore <= 0 ? 100 : request.MaxScore,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.CourseAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/catalog/courses/{id}/assignments/{assignment.Id}",
            new AssignmentSummaryDto(assignment.Id, assignment.Title, assignment.Instructions, assignment.MaxScore, false, null, null, 0));
    }

    private static async Task<IResult> SubmitAssignment(
        Guid id,
        Guid assignmentId,
        SubmitAssignmentRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var assignment = await db.CourseAssignments.AsNoTracking()
            .Include(a => a.Course)
            .SingleOrDefaultAsync(a => a.Id == assignmentId && a.CourseId == id, ct);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        var enrolled = await enrollment.IsConfirmedAsync(studentId, id, ct);
        if (!enrolled && !CatalogEndpoints.IsOwner(assignment.Course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest(new { error = "Write a submission before sending." });
        }

        var existing = await db.CourseAssignmentSubmissions
            .SingleOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId, ct);
        if (existing is null)
        {
            existing = new CourseAssignmentSubmission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                CourseId = id,
                StudentId = studentId,
                StudentName = CatalogEndpoints.DisplayName(user),
                Body = request.Body.Trim(),
                SubmittedAt = DateTimeOffset.UtcNow,
            };
            db.CourseAssignmentSubmissions.Add(existing);
        }
        else
        {
            existing.Body = request.Body.Trim();
            existing.SubmittedAt = DateTimeOffset.UtcNow;
            existing.Score = null;
            existing.Feedback = null;
            existing.GradedAt = null;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToDto(existing));
    }

    private static async Task<IResult> ListSubmissions(
        Guid id,
        Guid assignmentId,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        if (!CatalogEndpoints.IsOwner(course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        var rows = await db.CourseAssignmentSubmissions.AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId && s.CourseId == id)
            .ToListAsync(ct);
        return Results.Ok(rows.OrderByDescending(s => s.SubmittedAt).Select(ToDto).ToList());
    }

    private static async Task<IResult> GradeSubmission(
        Guid id,
        Guid assignmentId,
        Guid submissionId,
        GradeAssignmentRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        if (!CatalogEndpoints.IsOwner(course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        var assignment = await db.CourseAssignments.AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == assignmentId && a.CourseId == id, ct);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        var submission = await db.CourseAssignmentSubmissions
            .SingleOrDefaultAsync(s => s.Id == submissionId && s.AssignmentId == assignmentId, ct);
        if (submission is null)
        {
            return Results.NotFound();
        }

        submission.Score = Math.Clamp(request.Score, 0, assignment.MaxScore);
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback) ? null : request.Feedback.Trim();
        submission.GradedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToDto(submission));
    }

    private static AssignmentSubmissionDto ToDto(CourseAssignmentSubmission s) =>
        new(s.Id, s.AssignmentId, s.StudentId, s.StudentName, s.Body, s.Score, s.Feedback, s.SubmittedAt, s.GradedAt);
}
