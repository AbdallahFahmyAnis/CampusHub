using System.Security.Claims;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

public static class CourseLearningEndpoints
{
    public static RouteGroupBuilder MapCourseLearningEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/curriculum", GetCurriculum);
        api.MapGet("/courses/{id:guid}/lectures/{lectureId:guid}", GetLecture);
        api.MapPost("/courses/{id:guid}/sections", CreateSection).RequireAuthorization("CanManageCatalog");
        api.MapPost("/courses/{id:guid}/sections/{sectionId:guid}/lectures", CreateLecture).RequireAuthorization("CanManageCatalog");

        api.MapGet("/courses/{id:guid}/reviews", ListReviews);
        api.MapPost("/courses/{id:guid}/reviews", CreateReview);

        api.MapGet("/courses/{id:guid}/questions", ListQuestions);
        api.MapPost("/courses/{id:guid}/questions", CreateQuestion);
        api.MapPost("/courses/{id:guid}/questions/{questionId:guid}/answers", CreateAnswer);
        return api;
    }

    private static async Task<IResult> GetCurriculum(Guid id, CatalogDbContext db, CancellationToken ct)
    {
        if (!await CourseVisible(db, id, ct))
        {
            return Results.NotFound();
        }

        var sections = await db.CourseSections.AsNoTracking()
            .Where(s => s.CourseId == id)
            .Include(s => s.Lectures)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        var dto = new CurriculumDto(
            id,
            sections.Select(section => new SectionDto(
                section.Id,
                section.Title,
                section.SortOrder,
                section.Lectures.OrderBy(l => l.SortOrder).Select(CatalogMappings.ToOutline).ToList())).ToList());
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetLecture(
        Guid id,
        Guid lectureId,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var lecture = await db.Lectures
            .AsNoTracking()
            .Include(l => l.Section)
            .ThenInclude(s => s.Course)
            .SingleOrDefaultAsync(l => l.Id == lectureId && l.Section.CourseId == id, ct);
        if (lecture is null)
        {
            return Results.NotFound();
        }

        var course = lecture.Section.Course;
        var (studentId, _) = CatalogEndpoints.Caller(user);
        var unlocked = lecture.IsPreview
                       || CatalogEndpoints.CanManage(user)
                       || CatalogEndpoints.IsOwner(course, user)
                       || await enrollment.IsConfirmedAsync(studentId, id, ct);

        return Results.Ok(new LectureDetailDto(
            lecture.Id,
            lecture.SectionId,
            id,
            lecture.Title,
            lecture.Kind,
            lecture.DurationMinutes,
            lecture.Summary,
            unlocked ? lecture.Body : null,
            lecture.IsPreview,
            !unlocked,
            lecture.SortOrder));
    }

    private static async Task<IResult> CreateSection(
        Guid id,
        CreateSectionRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        if (!CatalogEndpoints.IsOwner(course, user) && !user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new { error = "Title is required." });
        }

        var order = await db.CourseSections.CountAsync(s => s.CourseId == id, ct) + 1;
        var section = new CourseSection
        {
            Id = Guid.NewGuid(),
            CourseId = id,
            Title = request.Title.Trim(),
            SortOrder = order
        };
        db.CourseSections.Add(section);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/catalog/courses/{id}/curriculum", new SectionDto(section.Id, section.Title, section.SortOrder, []));
    }

    private static async Task<IResult> CreateLecture(
        Guid id,
        Guid sectionId,
        CreateLectureRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var section = await db.CourseSections.Include(s => s.Course).SingleOrDefaultAsync(s => s.Id == sectionId && s.CourseId == id, ct);
        if (section is null)
        {
            return Results.NotFound();
        }

        if (!CatalogEndpoints.IsOwner(section.Course, user) && !user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new { error = "Title is required." });
        }

        var order = await db.Lectures.CountAsync(l => l.SectionId == sectionId, ct) + 1;
        var lecture = new Lecture
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Title = request.Title.Trim(),
            Kind = string.IsNullOrWhiteSpace(request.Kind) ? "Article" : request.Kind.Trim(),
            DurationMinutes = Math.Max(1, request.DurationMinutes),
            Summary = request.Summary?.Trim(),
            Body = request.Body?.Trim(),
            IsPreview = request.IsPreview,
            SortOrder = order
        };
        db.Lectures.Add(lecture);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/catalog/courses/{id}/lectures/{lecture.Id}", CatalogMappings.ToOutline(lecture));
    }

    private static async Task<IResult> ListReviews(Guid id, CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!await CourseVisible(db, id, ct))
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        var items = await db.CourseReviews.AsNoTracking()
            .Where(r => r.CourseId == id)
            .ToListAsync(ct);
        return Results.Ok(items
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => CatalogMappings.ToReview(r, studentId)));
    }

    private static async Task<IResult> CreateReview(
        Guid id,
        CreateReviewRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        if (!await enrollment.IsConfirmedAsync(studentId, id, ct) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Json(new { error = "Enroll in the course before leaving a review." }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest(new { error = "A rating from 1 to 5 and a review body are required." });
        }

        var existing = await db.CourseReviews.SingleOrDefaultAsync(r => r.CourseId == id && r.StudentId == studentId, ct);
        if (existing is null)
        {
            existing = new CourseReview
            {
                Id = Guid.NewGuid(),
                CourseId = id,
                StudentId = studentId,
                StudentName = CatalogEndpoints.DisplayName(user),
                Rating = request.Rating,
                Title = request.Title?.Trim(),
                Body = request.Body.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.CourseReviews.Add(existing);
        }
        else
        {
            existing.Rating = request.Rating;
            existing.Title = request.Title?.Trim();
            existing.Body = request.Body.Trim();
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(CatalogMappings.ToReview(existing, studentId));
    }

    private static async Task<IResult> ListQuestions(Guid id, CatalogDbContext db, CancellationToken ct)
    {
        if (!await CourseVisible(db, id, ct))
        {
            return Results.NotFound();
        }

        var items = await db.CourseQuestions.AsNoTracking()
            .Where(q => q.CourseId == id)
            .Include(q => q.Answers)
            .ToListAsync(ct);
        return Results.Ok(items
            .OrderByDescending(q => q.CreatedAt)
            .Select(CatalogMappings.ToQuestion));
    }

    private static async Task<IResult> CreateQuestion(
        Guid id,
        CreateQuestionRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        if (!await enrollment.IsConfirmedAsync(studentId, id, ct) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Json(new { error = "Enroll in the course before asking a question." }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest(new { error = "Title and body are required." });
        }

        var question = new CourseQuestion
        {
            Id = Guid.NewGuid(),
            CourseId = id,
            AuthorId = studentId,
            AuthorName = CatalogEndpoints.DisplayName(user),
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CourseQuestions.Add(question);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/catalog/courses/{id}/questions/{question.Id}", CatalogMappings.ToQuestion(question));
    }

    private static async Task<IResult> CreateAnswer(
        Guid id,
        Guid questionId,
        CreateAnswerRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var question = await db.CourseQuestions
            .Include(q => q.Course)
            .Include(q => q.Answers)
            .SingleOrDefaultAsync(q => q.Id == questionId && q.CourseId == id, ct);
        if (question is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        var isStaff = CatalogEndpoints.CanManage(user) || CatalogEndpoints.IsOwner(question.Course, user);
        if (!isStaff && !await enrollment.IsConfirmedAsync(studentId, id, ct))
        {
            return Results.Json(new { error = "Enroll in the course before answering." }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest(new { error = "An answer is required." });
        }

        var answer = new CourseAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            AuthorId = studentId,
            AuthorName = CatalogEndpoints.DisplayName(user),
            Body = request.Body.Trim(),
            IsTeacher = isStaff,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CourseAnswers.Add(answer);
        await db.SaveChangesAsync(ct);
        question.Answers.Add(answer);
        return Results.Ok(CatalogMappings.ToQuestion(question));
    }

    private static Task<bool> CourseVisible(CatalogDbContext db, Guid id, CancellationToken ct) =>
        db.Courses.AsNoTracking().AnyAsync(c => c.Id == id, ct);
}
