using System.Security.Claims;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

/// <summary>SDD CH-S13 / MDP-24 — specs/015-lecture-notes. Per-lecture student notes.</summary>
public static class NoteEndpoints
{
    public static RouteGroupBuilder MapNoteEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/notes/mine", ListMine);
        api.MapGet("/courses/{id:guid}/lectures/{lectureId:guid}/notes", GetNote);
        api.MapPut("/courses/{id:guid}/lectures/{lectureId:guid}/notes", SaveNote);
        return api;
    }

    private static async Task<IResult> ListMine(
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (studentId, _) = CatalogEndpoints.Caller(user);
        List<LectureNote> notes;
        try
        {
            notes = await db.LectureNotes.AsNoTracking()
                .Where(n => n.StudentId == studentId && n.Body != "")
                .ToListAsync(ct);
        }
        catch
        {
            return Results.Ok(Array.Empty<LectureNoteListItemDto>());
        }

        notes = [.. notes.OrderByDescending(n => n.UpdatedAt).Take(40)];
        if (notes.Count == 0)
        {
            return Results.Ok(Array.Empty<LectureNoteListItemDto>());
        }

        var courseIds = notes.Select(n => n.CourseId).Distinct().ToList();
        var lectureIds = notes.Select(n => n.LectureId).Distinct().ToList();
        var courses = await db.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Title })
            .ToListAsync(ct);
        var lectures = await db.Lectures.AsNoTracking()
            .Where(l => lectureIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Title })
            .ToListAsync(ct);

        var result = notes.Select(n => new LectureNoteListItemDto(
            n.CourseId,
            courses.FirstOrDefault(c => c.Id == n.CourseId)?.Title ?? "Course",
            n.LectureId,
            lectures.FirstOrDefault(l => l.Id == n.LectureId)?.Title ?? "Lecture",
            Snippet(n.Body),
            n.UpdatedAt)).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetNote(
        Guid id,
        Guid lectureId,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var lecture = await db.Lectures.AsNoTracking()
            .Include(l => l.Section)
            .SingleOrDefaultAsync(l => l.Id == lectureId && l.Section.CourseId == id, ct);
        if (lecture is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        LectureNote? note;
        try
        {
            note = await db.LectureNotes.AsNoTracking()
                .SingleOrDefaultAsync(n => n.CourseId == id && n.LectureId == lectureId && n.StudentId == studentId, ct);
        }
        catch
        {
            note = null;
        }

        return Results.Ok(new LectureNoteDto(id, lectureId, note?.Body ?? "", note?.UpdatedAt));
    }

    private static async Task<IResult> SaveNote(
        Guid id,
        Guid lectureId,
        SaveLectureNoteRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var lecture = await db.Lectures
            .Include(l => l.Section)
            .ThenInclude(s => s.Course)
            .SingleOrDefaultAsync(l => l.Id == lectureId && l.Section.CourseId == id, ct);
        if (lecture is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        var staff = CatalogEndpoints.CanManage(user) || CatalogEndpoints.IsOwner(lecture.Section.Course, user);
        if (!staff && !await enrollment.IsConfirmedAsync(studentId, id, ct))
        {
            return Results.Json(new { error = "Enroll in the course before saving notes." }, statusCode: StatusCodes.Status403Forbidden);
        }

        var body = request.Body ?? "";
        LectureNote? existing;
        try
        {
            existing = await db.LectureNotes
                .SingleOrDefaultAsync(n => n.CourseId == id && n.LectureId == lectureId && n.StudentId == studentId, ct);
        }
        catch
        {
            return Results.Problem("Lecture notes are not available yet.");
        }

        if (existing is null)
        {
            existing = new LectureNote
            {
                Id = Guid.NewGuid(),
                CourseId = id,
                LectureId = lectureId,
                StudentId = studentId,
                Body = body,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.LectureNotes.Add(existing);
        }
        else
        {
            existing.Body = body;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new LectureNoteDto(id, lectureId, existing.Body, existing.UpdatedAt));
    }

    private static string Snippet(string body)
    {
        var text = body.ReplaceLineEndings(" ").Trim();
        return text.Length <= 140 ? text : text[..137] + "…";
    }
}
