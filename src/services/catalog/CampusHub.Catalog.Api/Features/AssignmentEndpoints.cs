using System.Security.Claims;
using CampusHub.BuildingBlocks.Sdd;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

/// <summary>SDD CH-S12 / MDP-23 (assignments) and CH-S16 / MDP-27 (due dates + calendar). specs/014-assignments, specs/002-assignment-due-dates.</summary>
public static class AssignmentEndpoints
{
    public static RouteGroupBuilder MapAssignmentEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/assignments", ListAssignments);
        api.MapGet("/calendar", ListCalendar);
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
                staff ? count : 0,
                a.DueAt,
                AssignmentDueRules.Overdue(a.DueAt, mine is not null),
                AssignmentDueRules.Late(a.DueAt, mine?.SubmittedAt));
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
            DueAt = request.DueAt,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.CourseAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/catalog/courses/{id}/assignments/{assignment.Id}",
            new AssignmentSummaryDto(
                assignment.Id,
                assignment.Title,
                assignment.Instructions,
                assignment.MaxScore,
                false,
                null,
                null,
                0,
                assignment.DueAt,
                AssignmentDueRules.Overdue(assignment.DueAt, false),
                false));
    }

    private static async Task<IResult> ListCalendar(
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (studentId, _) = CatalogEndpoints.Caller(user);
        if (string.IsNullOrEmpty(studentId))
        {
            return Results.Unauthorized();
        }

        var progressIds = await db.LectureProgress.AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .Select(p => p.CourseId)
            .Distinct()
            .ToListAsync(ct);
        List<Guid> submissionIds;
        try
        {
            submissionIds = await db.CourseAssignmentSubmissions.AsNoTracking()
                .Where(s => s.StudentId == studentId)
                .Select(s => s.CourseId)
                .Distinct()
                .ToListAsync(ct);
        }
        catch
        {
            submissionIds = [];
        }

        var courseIds = progressIds.Concat(submissionIds).Distinct().ToList();
        if (courseIds.Count == 0)
        {
            return Results.Ok(Array.Empty<CalendarItemDto>());
        }

        List<CourseAssignment> assignments;
        try
        {
            assignments = await db.CourseAssignments.AsNoTracking()
                .Where(a => courseIds.Contains(a.CourseId))
                .ToListAsync(ct);
        }
        catch
        {
            return Results.Ok(Array.Empty<CalendarItemDto>());
        }

        assignments = [.. assignments.Where(a => a.DueAt is not null)];
        var titles = await db.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Title })
            .ToListAsync(ct);
        var titleById = titles.ToDictionary(c => c.Id, c => c.Title);

        List<CourseAssignmentSubmission> mine;
        try
        {
            mine = await db.CourseAssignmentSubmissions.AsNoTracking()
                .Where(s => s.StudentId == studentId && courseIds.Contains(s.CourseId))
                .ToListAsync(ct);
        }
        catch
        {
            mine = [];
        }

        var items = assignments
            .Select(a =>
            {
                var sub = mine.FirstOrDefault(s => s.AssignmentId == a.Id);
                return new CalendarItemDto(
                    a.CourseId,
                    titleById.GetValueOrDefault(a.CourseId, "Course"),
                    a.Id,
                    a.Title,
                    a.DueAt!.Value,
                    sub is not null,
                    AssignmentDueRules.Overdue(a.DueAt, sub is not null),
                    AssignmentDueRules.Late(a.DueAt, sub?.SubmittedAt));
            })
            .OrderBy(i => i.DueAt)
            .ToList();
        return Results.Ok(items);
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
