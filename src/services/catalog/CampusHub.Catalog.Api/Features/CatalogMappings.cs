using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

internal sealed record CourseStats(double Average, int Count, int Lectures, int Minutes);

internal static class CatalogMappings
{
    public static async Task<Dictionary<Guid, CourseStats>> LoadStats(
        CatalogDbContext db,
        IEnumerable<Guid> ids,
        CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var ratings = await db.CourseReviews.AsNoTracking()
            .Where(r => idList.Contains(r.CourseId))
            .GroupBy(r => r.CourseId)
            .Select(g => new { g.Key, Avg = g.Average(x => x.Rating), Count = g.Count() })
            .ToListAsync(ct);

        var lectures = await (
                from lecture in db.Lectures.AsNoTracking()
                join section in db.CourseSections.AsNoTracking() on lecture.SectionId equals section.Id
                where idList.Contains(section.CourseId)
                group lecture by section.CourseId into g
                select new { g.Key, Count = g.Count(), Minutes = g.Sum(x => x.DurationMinutes) })
            .ToListAsync(ct);

        var map = new Dictionary<Guid, CourseStats>();
        foreach (var id in idList)
        {
            var rating = ratings.FirstOrDefault(x => x.Key == id);
            var lecture = lectures.FirstOrDefault(x => x.Key == id);
            map[id] = new CourseStats(
                rating?.Avg ?? 0,
                rating?.Count ?? 0,
                lecture?.Count ?? 0,
                lecture?.Minutes ?? 0);
        }

        return map;
    }
    public static IReadOnlyList<string> Lines(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(['\n', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static CourseListItemDto ToListItem(Course c, CourseStats? stats)
    {
        var s = stats ?? new CourseStats(0, 0, 0, 0);
        return new(
            c.Id,
            c.Title,
            c.Subtitle,
            c.Subject.Code,
            c.Subject.Name,
            c.TeacherName,
            c.Capacity,
            c.RemainingSeats,
            c.Price,
            c.Status.ToString(),
            c.Level,
            Math.Round(s.Average, 1),
            s.Count,
            s.Lectures,
            s.Minutes);
    }

    public static CourseDetailDto ToDetail(Course c, CourseStats? stats, bool enrolled)
    {
        var s = stats ?? new CourseStats(0, 0, 0, 0);
        return new(
            c.Id,
            c.SubjectId,
            c.Title,
            c.Subtitle,
            c.Description,
            c.Subject.Code,
            c.Subject.Name,
            c.TeacherId,
            c.TeacherName,
            c.TeacherEmail,
            c.Capacity,
            c.RemainingSeats,
            c.Price,
            c.Status.ToString(),
            c.CanEnroll,
            enrolled,
            c.Level,
            c.Language,
            Lines(c.Outcomes),
            Lines(c.Requirements),
            Math.Round(s.Average, 1),
            s.Count,
            s.Lectures,
            s.Minutes);
    }

    public static LectureOutlineDto ToOutline(Lecture lecture) =>
        new(lecture.Id, lecture.Title, lecture.Kind, lecture.DurationMinutes, lecture.Summary, lecture.IsPreview, lecture.SortOrder);

    public static ReviewDto ToReview(CourseReview review, string studentId) =>
        new(review.Id, review.StudentName, review.Rating, review.Title, review.Body, review.CreatedAt, review.StudentId == studentId);

    public static QuestionDto ToQuestion(CourseQuestion question) =>
        new(
            question.Id,
            question.AuthorName,
            question.Title,
            question.Body,
            question.CreatedAt,
            question.Answers
                .OrderBy(a => a.CreatedAt)
                .Select(a => new AnswerDto(a.Id, a.AuthorName, a.Body, a.IsTeacher, a.CreatedAt))
                .ToList());
}
