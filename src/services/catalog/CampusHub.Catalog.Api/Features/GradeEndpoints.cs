using System.Security.Claims;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

public static class GradeEndpoints
{
    public static RouteGroupBuilder MapGradeEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/gradebook", GetGradebook).RequireAuthorization("CanManageCatalog");
        api.MapGet("/courses/{id:guid}/grades", GetMyGrades);
        return api;
    }

    private static async Task<IResult> GetGradebook(
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

        if (!CatalogEndpoints.IsOwner(course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        return Results.Ok(await BuildAsync(db, course, studentId: null, displayName: null, ct));
    }

    private static async Task<IResult> GetMyGrades(
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
        if (string.IsNullOrEmpty(studentId))
        {
            return Results.Unauthorized();
        }

        var book = await BuildAsync(db, course, studentId, CatalogEndpoints.DisplayName(user), ct);
        return Results.Ok(book);
    }

    private static async Task<GradebookDto> BuildAsync(
        CatalogDbContext db,
        Course course,
        string? studentId,
        string? displayName,
        CancellationToken ct)
    {
        List<CourseQuiz> quizzes;
        List<CourseAssignment> assignments;
        try
        {
            quizzes = await db.CourseQuizzes.AsNoTracking().Where(q => q.CourseId == course.Id).ToListAsync(ct);
        }
        catch
        {
            quizzes = [];
        }

        try
        {
            assignments = await db.CourseAssignments.AsNoTracking().Where(a => a.CourseId == course.Id).ToListAsync(ct);
        }
        catch
        {
            assignments = [];
        }

        quizzes = [.. quizzes.OrderBy(q => q.CreatedAt)];
        assignments = [.. assignments.OrderBy(a => a.CreatedAt)];

        List<CourseQuizAttempt> attempts;
        List<CourseAssignmentSubmission> submissions;
        try
        {
            attempts = await db.CourseQuizAttempts.AsNoTracking()
                .Where(a => a.CourseId == course.Id)
                .ToListAsync(ct);
        }
        catch
        {
            attempts = [];
        }

        try
        {
            submissions = await db.CourseAssignmentSubmissions.AsNoTracking()
                .Where(s => s.CourseId == course.Id)
                .ToListAsync(ct);
        }
        catch
        {
            submissions = [];
        }

        if (studentId is not null)
        {
            attempts = [.. attempts.Where(a => a.StudentId == studentId)];
            submissions = [.. submissions.Where(s => s.StudentId == studentId)];
        }

        var columns = new List<GradebookColumnDto>();
        columns.AddRange(quizzes.Select(q => new GradebookColumnDto("quiz", q.Id, q.Title, 100)));
        columns.AddRange(assignments.Select(a => new GradebookColumnDto("assignment", a.Id, a.Title, a.MaxScore)));

        var names = submissions
            .GroupBy(s => s.StudentId)
            .ToDictionary(g => g.Key, g => g.MaxBy(x => x.SubmittedAt)!.StudentName);

        try
        {
            var reviews = await db.CourseReviews.AsNoTracking()
                .Select(r => new { r.StudentId, r.StudentName })
                .ToListAsync(ct);
            foreach (var review in reviews)
            {
                names.TryAdd(review.StudentId, review.StudentName);
            }
        }
        catch
        {
            // ignore
        }

        var studentIds = attempts.Select(a => a.StudentId)
            .Concat(submissions.Select(s => s.StudentId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => names.GetValueOrDefault(id, id), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (studentId is not null && studentIds.Count == 0)
        {
            studentIds.Add(studentId);
            names.TryAdd(studentId, displayName ?? DisplayNamePlaceholder(studentId));
        }

        var rows = studentIds.Select(sid =>
        {
            var cells = new List<GradebookCellDto>();
            foreach (var quiz in quizzes)
            {
                var mine = attempts.Where(a => a.QuizId == quiz.Id && a.StudentId == sid).ToList();
                var best = mine.Count == 0 ? (int?)null : mine.Max(a => Percent(a.Score, a.Total));
                cells.Add(new GradebookCellDto(quiz.Id, best, 100, mine.Count > 0));
            }

            foreach (var assignment in assignments)
            {
                var mine = submissions.FirstOrDefault(s => s.AssignmentId == assignment.Id && s.StudentId == sid);
                cells.Add(new GradebookCellDto(assignment.Id, mine?.Score, assignment.MaxScore, mine is not null));
            }

            var scored = cells.Where(c => c.Score is not null).ToList();
            double? percent = scored.Count == 0
                ? null
                : Math.Round(scored.Sum(c => 100.0 * c.Score!.Value / Math.Max(1, c.MaxScore)) / scored.Count, 1);

            var name = names.GetValueOrDefault(sid) ?? DisplayNamePlaceholder(sid);
            return new GradebookRowDto(sid, name, cells, percent);
        }).ToList();

        return new GradebookDto(course.Id, course.Title, columns, rows);
    }

    private static int Percent(int score, int total) =>
        total <= 0 ? 0 : (int)Math.Round(score * 100.0 / total);

    private static string DisplayNamePlaceholder(string studentId) =>
        studentId.Contains('@', StringComparison.Ordinal) ? studentId : "Student";
}
