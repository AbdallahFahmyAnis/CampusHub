using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CampusHub.Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Infrastructure;

public sealed class CourseSearch(HttpClient http, IConfiguration config, ILogger<CourseSearch> logger)
{
    private static DateTimeOffset SkipUntil;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool Enabled => !string.IsNullOrWhiteSpace(config["MeiliSearch:Url"]);

    public async Task<SearchPage?> TrySearchAsync(
        string query,
        string? category,
        bool publishedOnly,
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (!Enabled || DateTimeOffset.UtcNow < SkipUntil)
        {
            return null;
        }

        try
        {
            var filters = new List<string> { $"tenantId = \"{tenantId}\"" };
            if (publishedOnly)
            {
                filters.Add("status = Published");
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                filters.Add($"subjectCode = \"{category.Trim().ToUpperInvariant()}\"");
            }

            var payload = new
            {
                q = query,
                limit = pageSize,
                offset = (page - 1) * pageSize,
                filter = filters.Count == 0 ? null : string.Join(" AND ", filters)
            };
            using var response = await http.PostAsJsonAsync("/indexes/courses/search", payload, Json, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<MeiliSearchResponse>(Json, ct);
            if (body is null)
            {
                return null;
            }

            var ids = body.Hits
                .Select(hit => Guid.TryParse(hit.Id, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();
            return new SearchPage(ids, body.EstimatedTotalHits);
        }
        catch (Exception ex)
        {
            SkipUntil = DateTimeOffset.UtcNow.AddSeconds(30);
            logger.LogWarning(ex, "Meilisearch query failed; using SQL search.");
            return null;
        }
    }

    public async Task RebuildAsync(CatalogDbContext db, CancellationToken ct)
    {
        if (!Enabled || DateTimeOffset.UtcNow < SkipUntil)
        {
            return;
        }

        try
        {
            await EnsureIndexAsync(ct);
            var courses = await db.Courses.AsNoTracking().Include(c => c.Subject).ToListAsync(ct);
            if (courses.Count == 0)
            {
                return;
            }

            using var response = await http.PutAsJsonAsync(
                "/indexes/courses/documents?primaryKey=id",
                courses.Select(ToDocument),
                Json,
                ct);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Indexed {Count} courses into Meilisearch", courses.Count);
        }
        catch (Exception ex)
        {
            SkipUntil = DateTimeOffset.UtcNow.AddSeconds(30);
            logger.LogWarning(ex, "Meilisearch rebuild skipped.");
        }
    }

    public async Task UpsertAsync(Course course, CancellationToken ct)
    {
        if (!Enabled || DateTimeOffset.UtcNow < SkipUntil)
        {
            return;
        }

        try
        {
            using var response = await http.PutAsJsonAsync(
                "/indexes/courses/documents?primaryKey=id",
                new[] { ToDocument(course) },
                Json,
                ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            SkipUntil = DateTimeOffset.UtcNow.AddSeconds(30);
            logger.LogWarning(ex, "Meilisearch upsert failed for {CourseId}", course.Id);
        }
    }

    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        await http.PostAsJsonAsync("/indexes", new { uid = "courses", primaryKey = "id" }, Json, ct);
        using var settings = await http.PatchAsJsonAsync(
            "/indexes/courses/settings",
            new
            {
                searchableAttributes = new[]
                {
                    "title", "subtitle", "description", "subjectName", "subjectCode", "teacherName", "level", "outcomes"
                },
                filterableAttributes = new[] { "status", "subjectCode", "tenantId" },
                displayedAttributes = new[] { "id", "title", "subjectCode" }
            },
            Json,
            ct);
        settings.EnsureSuccessStatusCode();
    }

    private static CourseSearchDocument ToDocument(Course course) =>
        new(
            course.Id.ToString(),
            course.Title,
            course.Subtitle ?? "",
            course.Description ?? "",
            course.Subject.Code,
            course.Subject.Name,
            course.TeacherName,
            course.Level ?? "",
            course.Outcomes ?? "",
            course.Status.ToString(),
            course.TenantId.ToString());

    public sealed record SearchPage(IReadOnlyList<Guid> Ids, int Total);

    private sealed record CourseSearchDocument(
        string Id,
        string Title,
        string Subtitle,
        string Description,
        string SubjectCode,
        string SubjectName,
        string TeacherName,
        string Level,
        string Outcomes,
        string Status,
        string TenantId);

    private sealed class MeiliSearchResponse
    {
        public List<MeiliHit> Hits { get; set; } = [];
        public int EstimatedTotalHits { get; set; }
    }

    private sealed class MeiliHit
    {
        public string Id { get; set; } = "";
    }
}
