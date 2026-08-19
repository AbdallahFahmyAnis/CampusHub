using System.Security.Claims;
using CampusHub.BuildingBlocks.Security;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/catalog").RequireAuthorization();

        api.MapGet("/subjects", ListSubjects);
        api.MapPost("/subjects", CreateSubject).RequireAuthorization("CanManageCatalog");

        api.MapGet("/capabilities", GetCapabilities);
        api.MapGet("/courses", ListCourses);
        api.MapGet("/courses/mine", ListMine).RequireAuthorization("CanManageCatalog");
        api.MapGet("/courses/recommended", Recommended).RequireAuthorization();
        api.MapGet("/courses/{id:guid}", GetCourse);
        api.MapPost("/courses", CreateCourse).RequireAuthorization("CanManageCatalog");
        api.MapPut("/courses/{id:guid}", UpdateCourse).RequireAuthorization("CanManageCatalog");
        api.MapPost("/courses/{id:guid}/publish", PublishCourse).RequireAuthorization("CanManageCatalog");
        api.MapPost("/courses/{id:guid}/archive", ArchiveCourse).RequireAuthorization("CanManageCatalog");

        api.MapPost("/courses/{id:guid}/reservations", ReserveSeat).AllowAnonymous();
        api.MapDelete("/courses/{id:guid}/reservations", ReleaseSeat).AllowAnonymous();

        api.MapCourseLearningEndpoints();

        return app;
    }

    private static async Task<IResult> ListSubjects(CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var tenantId = Tenancy.TenantId(user);
        var items = await db.Subjects
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId || (tenantId == Tenancy.DefaultTenantId && s.TenantId == Guid.Empty))
            .OrderBy(s => s.Code)
            .Select(s => new SubjectDto(s.Id, s.Code, s.Name, s.Description))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateSubject(
        CreateSubjectRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Code and name are required." });
        }

        var tenantId = Tenancy.TenantId(user);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Subjects.AnyAsync(s => s.TenantId == tenantId && s.Code == code, ct))
        {
            return Results.Conflict(new { error = "A category with that code already exists on this campus." });
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        db.Subjects.Add(subject);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/catalog/subjects/{subject.Id}",
            new SubjectDto(subject.Id, subject.Code, subject.Name, subject.Description));
    }

    private static IResult GetCapabilities(CourseSearch search, CourseTutor tutor) =>
        Results.Ok(new CatalogCapabilitiesDto(
            search.Enabled ? "meilisearch" : "sql",
            tutor.ModelEnabled ? "model" : "catalog"));

    private static async Task<IResult> ListCourses(
        CatalogDbContext db,
        ClaimsPrincipal user,
        CourseSearch search,
        Guid? subjectId,
        string? category,
        string? q,
        string? level,
        decimal? minPrice,
        decimal? maxPrice,
        double? minRating,
        string? sortBy,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 48);
        var publishedOnly = !CanManage(user);

        if (!string.IsNullOrWhiteSpace(q) && subjectId is null)
        {
            var ranked = await search.TrySearchAsync(q.Trim(), category, publishedOnly, Tenancy.TenantId(user), page, pageSize, ct);
            if (ranked is not null)
            {
                return await PageFromIds(db, user, ranked.Ids, page, pageSize, ranked.Total, ct);
            }
        }

        var tenantId = Tenancy.TenantId(user);
        var query = db.Courses.AsNoTracking().Include(c => c.Subject)
            .Where(c => c.TenantId == tenantId || (c.TenantId == Guid.Empty && tenantId == Tenancy.DefaultTenantId));
        if (publishedOnly)
        {
            query = query.Where(c => c.Status == CourseStatus.Published);
        }

        if (subjectId is Guid id)
        {
            query = query.Where(c => c.SubjectId == id);
        }
        else if (!string.IsNullOrWhiteSpace(category))
        {
            var code = category.Trim().ToUpperInvariant();
            query = query.Where(c => c.Subject.Code == code);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c =>
                c.Title.Contains(term) ||
                (c.Subtitle != null && c.Subtitle.Contains(term)) ||
                (c.Description != null && c.Description.Contains(term)) ||
                c.Subject.Name.Contains(term) ||
                c.Subject.Code.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(c => c.Level == level.Trim());
        }

        if (minPrice.HasValue)
        {
            query = query.Where(c => c.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(c => c.Price <= maxPrice.Value);
        }

        var total = await query.CountAsync(ct);

        // Apply sort
        var ordered = sortBy switch
        {
            "price-asc" => query.OrderBy(c => c.Price),
            "price-desc" => query.OrderByDescending(c => c.Price),
            "newest" => query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Title),
        };

        var courses = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Post-filter by rating (stats loaded in memory after paging)
        if (minRating.HasValue)
        {
            var stats = await CatalogMappings.LoadStats(db, courses.Select(c => c.Id), ct);
            courses = courses
                .Where(c => stats.TryGetValue(c.Id, out var s) && s.Average >= minRating.Value)
                .ToList();
        }
        return await ToPaged(db, user, courses, page, pageSize, total, ct);
    }

    private static async Task<IResult> PageFromIds(
        CatalogDbContext db,
        ClaimsPrincipal user,
        IReadOnlyList<Guid> ids,
        int page,
        int pageSize,
        int total,
        CancellationToken ct)
    {
        var courses = await db.Courses.AsNoTracking()
            .Include(c => c.Subject)
            .Where(c => ids.Contains(c.Id) && (c.TenantId == Tenancy.TenantId(user) || c.TenantId == Guid.Empty))
            .ToListAsync(ct);
        var order = ids.Select((courseId, index) => (courseId, index)).ToDictionary(x => x.courseId, x => x.index);
        courses.Sort((a, b) => order[a.Id].CompareTo(order[b.Id]));
        return await ToPaged(db, user, courses, page, pageSize, total, ct);
    }

    private static async Task<IResult> ToPaged(
        CatalogDbContext db,
        ClaimsPrincipal user,
        List<Course> courses,
        int page,
        int pageSize,
        int total,
        CancellationToken ct)
    {
        var stats = await CatalogMappings.LoadStats(db, courses.Select(c => c.Id), ct);
        var (studentId, _) = Caller(user);
        var wished = await CatalogMappings.WishlistIds(db, studentId, courses.Select(c => c.Id), ct);
        return Results.Ok(new PagedCoursesDto(
            courses.Select(c => CatalogMappings.ToListItem(c, stats.GetValueOrDefault(c.Id), wished.Contains(c.Id))).ToList(),
            page,
            pageSize,
            total));
    }

    private static async Task<IResult> Recommended(
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var (studentId, _) = Caller(user);
        var tenantId = Tenancy.TenantId(user);

        // Find the subjects the student has already enrolled in
        var enrolledCourseIds = await db.CourseWishlists.AsNoTracking()
            .Where(w => w.StudentId == studentId)
            .Select(w => w.CourseId)
            .ToListAsync(ct);

        // Also collect subjects from progress (courses they've started learning)
        var startedCourseIds = await db.LectureProgress.AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .Select(p => p.CourseId)
            .Distinct()
            .ToListAsync(ct);

        var interactedIds = enrolledCourseIds.Union(startedCourseIds).Distinct().ToList();

        // Get the subject IDs of those courses
        var interactedSubjectIds = await db.Courses.AsNoTracking()
            .Where(c => interactedIds.Contains(c.Id))
            .Select(c => c.SubjectId)
            .Distinct()
            .ToListAsync(ct);

        IQueryable<Course> query;
        if (interactedSubjectIds.Count > 0)
        {
            // Return published courses in the same subjects the student hasn't interacted with yet
            query = db.Courses.AsNoTracking()
                .Include(c => c.Subject)
                .Where(c =>
                    c.TenantId == tenantId &&
                    c.Status == CourseStatus.Published &&
                    interactedSubjectIds.Contains(c.SubjectId) &&
                    !interactedIds.Contains(c.Id));
        }
        else
        {
            // No history — return highest-rated courses
            query = db.Courses.AsNoTracking()
                .Include(c => c.Subject)
                .Where(c => c.TenantId == tenantId && c.Status == CourseStatus.Published);
        }

        var candidates = await query.OrderBy(c => c.Title).Take(20).ToListAsync(ct);
        var stats = await CatalogMappings.LoadStats(db, candidates.Select(c => c.Id), ct);

        // Sort by rating desc, take top 6
        var ranked = candidates
            .OrderByDescending(c => stats.TryGetValue(c.Id, out var s) ? s.Average : 0)
            .Take(6)
            .ToList();

        var wished = await CatalogMappings.WishlistIds(db, studentId, ranked.Select(c => c.Id), ct);
        return Results.Ok(ranked.Select(c => CatalogMappings.ToListItem(c, stats.GetValueOrDefault(c.Id), wished.Contains(c.Id))).ToList());
    }

    private static async Task<IResult> ListMine(CatalogDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var (id, email) = Caller(user);
        var tenantId = Tenancy.TenantId(user);
        var courses = await db.Courses.AsNoTracking()
            .Include(c => c.Subject)
            .Where(c => c.TenantId == tenantId && (c.TeacherId == id || c.TeacherEmail == email))
            .OrderBy(c => c.Title)
            .ToListAsync(ct);
        var stats = await CatalogMappings.LoadStats(db, courses.Select(c => c.Id), ct);
        var wished = await CatalogMappings.WishlistIds(db, id, courses.Select(c => c.Id), ct);

        return Results.Ok(courses.Select(c => CatalogMappings.ToListItem(c, stats.GetValueOrDefault(c.Id), wished.Contains(c.Id))));
    }

    private static async Task<IResult> GetCourse(
        Guid id,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking()
            .Include(c => c.Subject)
            .SingleOrDefaultAsync(c => c.Id == id && (c.TenantId == Tenancy.TenantId(user) || c.TenantId == Guid.Empty), ct);

        if (course is null)
        {
            return Results.NotFound();
        }

        if (course.Status != CourseStatus.Published && !CanManage(user) && !IsOwner(course, user))
        {
            return Results.NotFound();
        }

        var (studentId, _) = Caller(user);
        var enrolled = CanManage(user) || IsOwner(course, user) || await enrollment.IsConfirmedAsync(studentId, id, ct);
        var wishlisted = await db.CourseWishlists.AnyAsync(w => w.CourseId == id && w.StudentId == studentId, ct);
        var stats = await CatalogMappings.LoadStats(db, [id], ct);
        return Results.Ok(CatalogMappings.ToDetail(course, stats.GetValueOrDefault(id), enrolled, wishlisted));
    }

    private static async Task<IResult> CreateCourse(
        CreateCourseRequest request,
        CatalogDbContext db,
        CourseSearch search,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (request.Capacity < 1)
        {
            return Results.BadRequest(new { error = "Capacity must be at least 1." });
        }

        var tenantId = Tenancy.TenantId(user);
        var subject = await db.Subjects.SingleOrDefaultAsync(
            s => s.Id == request.SubjectId && s.TenantId == tenantId, ct);
        if (subject is null)
        {
            return Results.BadRequest(new { error = "Unknown subject for this campus." });
        }

        var (id, email) = Caller(user);
        var course = new Course
        {
            Id = Guid.NewGuid(),
            SubjectId = subject.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            TeacherId = id,
            TeacherName = user.Identity?.Name ?? "Teacher",
            TeacherEmail = email,
            Capacity = request.Capacity,
            RemainingSeats = request.Capacity,
            Price = request.Price,
            Subtitle = request.Subtitle?.Trim(),
            Level = request.Level?.Trim(),
            Language = request.Language?.Trim(),
            Outcomes = request.Outcomes?.Trim(),
            Requirements = request.Requirements?.Trim(),
            Status = CourseStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            TenantId = Tenancy.TenantId(user)
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);
        course.Subject = subject;
        await search.UpsertAsync(course, ct);
        return Results.Created($"/api/catalog/courses/{course.Id}", CatalogMappings.ToDetail(course, null, true));
    }

    private static async Task<IResult> UpdateCourse(
        Guid id,
        UpdateCourseRequest request,
        CatalogDbContext db,
        CourseSearch search,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var course = await db.Courses.Include(c => c.Subject).SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null || course.TenantId != Tenancy.TenantId(user))
        {
            return Results.NotFound();
        }

        if (!IsOwner(course, user) && !user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        var tenantId = Tenancy.TenantId(user);
        var subject = await db.Subjects.SingleOrDefaultAsync(
            s => s.Id == request.SubjectId && s.TenantId == tenantId, ct);
        if (subject is null)
        {
            return Results.BadRequest(new { error = "Unknown subject for this campus." });
        }

        var occupied = course.Capacity - course.RemainingSeats;
        if (request.Capacity < occupied)
        {
            return Results.BadRequest(new { error = "Capacity cannot be below current enrollments." });
        }

        course.SubjectId = subject.Id;
        course.Title = request.Title.Trim();
        course.Description = request.Description?.Trim();
        course.Price = request.Price;
        course.Subtitle = request.Subtitle?.Trim();
        course.Level = request.Level?.Trim();
        course.Language = request.Language?.Trim();
        course.Outcomes = request.Outcomes?.Trim();
        course.Requirements = request.Requirements?.Trim();
        course.RemainingSeats += request.Capacity - course.Capacity;
        course.Capacity = request.Capacity;
        await db.SaveChangesAsync(ct);
        course.Subject = subject;
        await search.UpsertAsync(course, ct);
        return Results.Ok(CatalogMappings.ToDetail(course, null, true));
    }

    private static async Task<IResult> PublishCourse(Guid id, CatalogDbContext db, CourseSearch search, ClaimsPrincipal user, CancellationToken ct)
    {
        var course = await db.Courses.Include(c => c.Subject).SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null || course.TenantId != Tenancy.TenantId(user))
        {
            return Results.NotFound();
        }

        if (!IsOwner(course, user) && !user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        try
        {
            if (course.RemainingSeats == 0)
            {
                course.RemainingSeats = course.Capacity;
            }

            course.Publish();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        await db.SaveChangesAsync(ct);
        await search.UpsertAsync(course, ct);
        return Results.Ok(CatalogMappings.ToDetail(course, null, true));
    }

    private static async Task<IResult> ArchiveCourse(Guid id, CatalogDbContext db, CourseSearch search, ClaimsPrincipal user, CancellationToken ct)
    {
        var course = await db.Courses.Include(c => c.Subject).SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course is null || course.TenantId != Tenancy.TenantId(user))
        {
            return Results.NotFound();
        }

        if (!IsOwner(course, user) && !user.IsInRole(Roles.Administrator))
        {
            return Results.Forbid();
        }

        course.Status = CourseStatus.Archived;
        await db.SaveChangesAsync(ct);
        await search.UpsertAsync(course, ct);
        return Results.Ok(CatalogMappings.ToDetail(course, null, true));
    }

    private static async Task<IResult> ReserveSeat(Guid id, HttpContext http, IConfiguration config, CatalogDbContext db, CancellationToken ct)
    {
        if (!IsUserOrInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var updated = await db.Courses
            .Where(c => c.Id == id && c.Status == CourseStatus.Published && c.RemainingSeats > 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.RemainingSeats, c => c.RemainingSeats - 1), ct);

        return updated == 1
            ? Results.Ok(new { reserved = true })
            : Results.Conflict(new { error = "No seats remaining or course is not published." });
    }

    private static async Task<IResult> ReleaseSeat(Guid id, HttpContext http, IConfiguration config, CatalogDbContext db, CancellationToken ct)
    {
        if (!IsUserOrInternal(http, config))
        {
            return Results.Unauthorized();
        }

        var updated = await db.Courses
            .Where(c => c.Id == id && c.RemainingSeats < c.Capacity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.RemainingSeats, c => c.RemainingSeats + 1), ct);

        return updated == 1
            ? Results.Ok(new { released = true })
            : Results.Conflict(new { error = "Seat could not be released." });
    }

    private static bool IsUserOrInternal(HttpContext http, IConfiguration config)
    {
        var expected = config["Internal:ApiKey"] ?? "campus-dev-internal";
        if (http.Request.Headers.TryGetValue("X-Internal-Key", out var provided) &&
            string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
        {
            return true;
        }

        return http.User.Identity?.IsAuthenticated == true;
    }

    internal static bool CanManage(ClaimsPrincipal user) =>
        user.IsInRole(Roles.Teacher) || user.IsInRole(Roles.Administrator);

    internal static bool IsOwner(Course course, ClaimsPrincipal user)
    {
        var (id, email) = Caller(user);
        return course.TeacherId == id || string.Equals(course.TeacherEmail, email, StringComparison.OrdinalIgnoreCase);
    }

    internal static (string Id, string Email) Caller(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = user.FindFirstValue("email")
                    ?? user.FindFirstValue("preferred_username")
                    ?? user.Identity?.Name
                    ?? string.Empty;
        return (id, email);
    }

    internal static string DisplayName(ClaimsPrincipal user) =>
        user.FindFirstValue("name") ?? user.Identity?.Name ?? Caller(user).Email;
}
