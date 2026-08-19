using System.Security.Claims;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using CampusHub.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

public static class CourseLearningEndpoints
{
    public static RouteGroupBuilder MapCourseLearningEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/curriculum", GetCurriculum);
        api.MapGet("/courses/{id:guid}/lectures/{lectureId:guid}", GetLecture);
        api.MapPost("/courses/{id:guid}/lectures/{lectureId:guid}/complete", CompleteLecture);
        api.MapPost("/courses/{id:guid}/ask", AskCourse);
        api.MapPost("/courses/{id:guid}/sections", CreateSection).RequireAuthorization("CanManageCatalog");
        api.MapPost("/courses/{id:guid}/sections/{sectionId:guid}/lectures", CreateLecture).RequireAuthorization("CanManageCatalog");

        api.MapGet("/wishlist", ListWishlist);
        api.MapPost("/courses/{id:guid}/wishlist", AddWishlist);
        api.MapDelete("/courses/{id:guid}/wishlist", RemoveWishlist);

        api.MapGet("/courses/{id:guid}/reviews", ListReviews);
        api.MapPost("/courses/{id:guid}/reviews", CreateReview);

        api.MapGet("/courses/{id:guid}/questions", ListQuestions);
        api.MapPost("/courses/{id:guid}/questions", CreateQuestion);
        api.MapPost("/courses/{id:guid}/questions/{questionId:guid}/answers", CreateAnswer);
        return api;
    }

    private static async Task<IResult> GetCurriculum(Guid id, CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!await CourseVisible(db, id, user, ct))
        {
            return Results.NotFound();
        }

        var sections = await db.CourseSections.AsNoTracking()
            .Where(s => s.CourseId == id)
            .Include(s => s.Lectures)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
        var (studentId, _) = CatalogEndpoints.Caller(user);
        var completed = await CatalogMappings.CompletedLectureIds(db, studentId, id, ct);

        var dto = new CurriculumDto(
            id,
            sections.Select(section => new SectionDto(
                section.Id,
                section.Title,
                section.SortOrder,
                section.Lectures.OrderBy(l => l.SortOrder).Select(l => CatalogMappings.ToOutline(l, completed.Contains(l.Id))).ToList())).ToList());
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
        var completed = await db.LectureProgress.AnyAsync(
            p => p.LectureId == lectureId && p.StudentId == studentId, ct);

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
            lecture.SortOrder,
            unlocked ? lecture.VideoUrl : null,
            completed));
    }

    private static async Task<IResult> AskCourse(
        Guid id,
        AskCourseRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CourseTutor tutor,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return Results.BadRequest(new { error = "Ask a question about this course." });
        }

        var course = await db.Courses.Include(c => c.Subject).SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null || !await CourseVisible(db, id, user, ct))
        {
            return Results.NotFound();
        }

        Lecture? lecture = null;
        if (request.LectureId is Guid lectureId)
        {
            lecture = await db.Lectures
                .Include(l => l.Section)
                .SingleOrDefaultAsync(l => l.Id == lectureId && l.Section.CourseId == id, ct);
            if (lecture is not null)
            {
                var (studentId, _) = CatalogEndpoints.Caller(user);
                var unlocked = lecture.IsPreview
                               || CatalogEndpoints.CanManage(user)
                               || CatalogEndpoints.IsOwner(course, user)
                               || await enrollment.IsConfirmedAsync(studentId, id, ct);
                if (!unlocked)
                {
                    lecture.Body = null;
                }
            }
        }

        var allowModel = Plans.AllowsModelAi(Tenancy.Plan(user));
        var answer = await tutor.AnswerAsync(request.Question.Trim(), course, lecture, allowModel, ct);
        return Results.Ok(new AskCourseResponse(answer, allowModel && tutor.ModelEnabled ? "model" : "catalog"));
    }

    private static async Task<IResult> CompleteLecture(
        Guid id,
        Guid lectureId,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        EventPublisher events,
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
        var enrollmentId = await enrollment.GetEnrollmentIdAsync(studentId, id, ct);
        var allowed = lecture.IsPreview
                      || CatalogEndpoints.CanManage(user)
                      || CatalogEndpoints.IsOwner(lecture.Section.Course, user)
                      || enrollmentId is not null;
        if (!allowed)
        {
            return Results.Forbid();
        }

        var existing = await db.LectureProgress.SingleOrDefaultAsync(
            p => p.LectureId == lectureId && p.StudentId == studentId, ct);
        if (existing is null)
        {
            db.LectureProgress.Add(new LectureProgress
            {
                Id = Guid.NewGuid(),
                CourseId = id,
                LectureId = lectureId,
                StudentId = studentId,
                CompletedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        // Check if all lectures in the course are now completed
        var totalLectures = await db.Lectures.CountAsync(
            l => l.Section.CourseId == id, ct);
        var completedCount = await db.LectureProgress.CountAsync(
            p => p.CourseId == id && p.StudentId == studentId, ct);

        var courseComplete = totalLectures > 0 && completedCount >= totalLectures;
        if (courseComplete && enrollmentId is not null)
        {
            var course = lecture.Section.Course;
            var email = user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var name = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name) ?? studentId;
            await events.PublishAsync(EventTypes.CourseCompleted, new CourseCompletedV1(
                id, course.Title, studentId, email, name, enrollmentId.Value, DateTimeOffset.UtcNow), ct);
        }

        return Results.Ok(new { completed = true, courseComplete });
    }

    private static async Task<IResult> ListWishlist(CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (studentId, _) = CatalogEndpoints.Caller(user);
        var saved = await db.CourseWishlists.AsNoTracking()
            .Where(w => w.StudentId == studentId)
            .Select(w => new { w.CourseId, w.CreatedAt })
            .ToListAsync(ct);
        var ids = saved
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => w.CourseId)
            .ToList();
        var courses = await db.Courses.AsNoTracking()
            .Include(c => c.Subject)
            .Where(c => ids.Contains(c.Id) && c.TenantId == Tenancy.TenantId(user))
            .ToListAsync(ct);
        var order = ids.Select((courseId, index) => (courseId, index)).ToDictionary(x => x.courseId, x => x.index);
        courses.Sort((a, b) => order[a.Id].CompareTo(order[b.Id]));
        var stats = await CatalogMappings.LoadStats(db, ids, ct);
        return Results.Ok(courses.Select(c => CatalogMappings.ToListItem(c, stats.GetValueOrDefault(c.Id), true)).ToList());
    }

    private static async Task<IResult> AddWishlist(Guid id, CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!await CourseVisible(db, id, user, ct))
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        if (string.IsNullOrEmpty(studentId))
        {
            return Results.Unauthorized();
        }

        var exists = await db.CourseWishlists.AnyAsync(w => w.CourseId == id && w.StudentId == studentId, ct);
        if (!exists)
        {
            db.CourseWishlists.Add(new CourseWishlist
            {
                Id = Guid.NewGuid(),
                CourseId = id,
                StudentId = studentId,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new { wishlisted = true });
    }

    private static async Task<IResult> RemoveWishlist(Guid id, CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (studentId, _) = CatalogEndpoints.Caller(user);
        var rows = await db.CourseWishlists
            .Where(w => w.CourseId == id && w.StudentId == studentId)
            .ExecuteDeleteAsync(ct);
        return rows > 0 || await db.Courses.AnyAsync(c => c.Id == id, ct)
            ? Results.Ok(new { wishlisted = false })
            : Results.NotFound();
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
            VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl) ? null : request.VideoUrl.Trim(),
            IsPreview = request.IsPreview,
            SortOrder = order
        };
        db.Lectures.Add(lecture);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/catalog/courses/{id}/lectures/{lecture.Id}", CatalogMappings.ToOutline(lecture));
    }

    private static async Task<IResult> ListReviews(Guid id, CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!await CourseVisible(db, id, user, ct))
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

    private static async Task<IResult> ListQuestions(Guid id, CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!await CourseVisible(db, id, user, ct))
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

    private static Task<bool> CourseVisible(CatalogDbContext db, Guid id, ClaimsPrincipal user, CancellationToken ct)
    {
        var tenantId = Tenancy.TenantId(user);
        return db.Courses.AsNoTracking().AnyAsync(
            c => c.Id == id && (c.TenantId == tenantId || (c.TenantId == Guid.Empty && tenantId == Tenancy.DefaultTenantId)),
            ct);
    }
}
