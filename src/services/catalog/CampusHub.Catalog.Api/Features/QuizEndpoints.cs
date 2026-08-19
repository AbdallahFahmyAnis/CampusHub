using System.Security.Claims;
using System.Text.Json;
using CampusHub.BuildingBlocks.Sdd;
using CampusHub.Catalog.Api.Contracts;
using CampusHub.Catalog.Api.Domain;
using CampusHub.Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Features;

/// <summary>SDD CH-S11 / MDP-22 — specs/013-quizzes. Course quizzes and attempts.</summary>
public static class QuizEndpoints
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static RouteGroupBuilder MapQuizEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/courses/{id:guid}/quizzes", ListQuizzes);
        api.MapPost("/courses/{id:guid}/quizzes", CreateQuiz).RequireAuthorization("CanManageCatalog");
        api.MapGet("/courses/{id:guid}/quizzes/{quizId:guid}", GetQuiz);
        api.MapPost("/courses/{id:guid}/quizzes/{quizId:guid}/submit", SubmitQuiz);
        return api;
    }

    private static async Task<IResult> ListQuizzes(
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
        var quizzes = await db.CourseQuizzes.AsNoTracking()
            .Where(q => q.CourseId == id)
            .ToListAsync(ct);
        quizzes = [.. quizzes.OrderBy(q => q.CreatedAt)];

        List<CourseQuizAttempt> attempts;
        try
        {
            attempts = await db.CourseQuizAttempts.AsNoTracking()
                .Where(a => a.CourseId == id && a.StudentId == studentId)
                .ToListAsync(ct);
        }
        catch
        {
            attempts = [];
        }

        var result = quizzes.Select(quiz =>
        {
            var mine = attempts.Where(a => a.QuizId == quiz.Id).ToList();
            var best = mine.Count == 0 ? (int?)null : mine.Max(a => QuizScoring.Percent(a.Score, a.Total));
            var passed = mine.Any(a => a.Passed);
            var questions = ParseQuestions(quiz.QuestionsJson);
            return new QuizSummaryDto(quiz.Id, quiz.Title, quiz.PassPercent, questions.Count, best, mine.Count == 0 ? null : passed);
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> CreateQuiz(
        Guid id,
        CreateQuizRequest request,
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

        if (string.IsNullOrWhiteSpace(request.Title) || request.Questions is null || request.Questions.Count == 0)
        {
            return Results.BadRequest(new { error = "A quiz needs a title and at least one question." });
        }

        var questions = new List<StoredQuestion>();
        foreach (var q in request.Questions)
        {
            if (string.IsNullOrWhiteSpace(q.Prompt) || q.Choices is null || q.Choices.Count < 2)
            {
                return Results.BadRequest(new { error = "Each question needs a prompt and at least two choices." });
            }

            if (q.CorrectIndex < 0 || q.CorrectIndex >= q.Choices.Count)
            {
                return Results.BadRequest(new { error = "CorrectIndex is out of range." });
            }

            questions.Add(new StoredQuestion(
                Guid.NewGuid(),
                q.Prompt.Trim(),
                q.Choices.Select(c => c.Trim()).Where(c => c.Length > 0).ToList(),
                q.CorrectIndex));
        }

        var quiz = new CourseQuiz
        {
            Id = Guid.NewGuid(),
            CourseId = id,
            Title = request.Title.Trim(),
            PassPercent = Math.Clamp(request.PassPercent <= 0 ? 70 : request.PassPercent, 1, 100),
            QuestionsJson = JsonSerializer.Serialize(questions, Json),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.CourseQuizzes.Add(quiz);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/catalog/courses/{id}/quizzes/{quiz.Id}", ToDetail(quiz, revealAnswers: true, null, null));
    }

    private static async Task<IResult> GetQuiz(
        Guid id,
        Guid quizId,
        CatalogDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var quiz = await db.CourseQuizzes.AsNoTracking()
            .Include(q => q.Course)
            .SingleOrDefaultAsync(q => q.Id == quizId && q.CourseId == id, ct);
        if (quiz is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        var reveal = CatalogEndpoints.IsOwner(quiz.Course, user) || CatalogEndpoints.CanManage(user);
        var mine = await db.CourseQuizAttempts.AsNoTracking()
            .Where(a => a.QuizId == quizId && a.StudentId == studentId)
            .ToListAsync(ct);
        var best = mine.Count == 0 ? (int?)null : mine.Max(a => QuizScoring.Percent(a.Score, a.Total));
        var passed = mine.Count == 0 ? (bool?)null : mine.Any(a => a.Passed);
        return Results.Ok(ToDetail(quiz, reveal, best, passed));
    }

    private static async Task<IResult> SubmitQuiz(
        Guid id,
        Guid quizId,
        SubmitQuizRequest request,
        CatalogDbContext db,
        ClaimsPrincipal user,
        EnrollmentGateway enrollment,
        CancellationToken ct)
    {
        var quiz = await db.CourseQuizzes.AsNoTracking()
            .Include(q => q.Course)
            .SingleOrDefaultAsync(q => q.Id == quizId && q.CourseId == id, ct);
        if (quiz is null)
        {
            return Results.NotFound();
        }

        var (studentId, _) = CatalogEndpoints.Caller(user);
        var enrolled = await enrollment.IsConfirmedAsync(studentId, id, ct);
        if (!enrolled && !CatalogEndpoints.IsOwner(quiz.Course, user) && !CatalogEndpoints.CanManage(user))
        {
            return Results.Forbid();
        }

        var questions = ParseQuestions(quiz.QuestionsJson);
        var answers = request.Answers ?? [];
        var score = 0;
        foreach (var question in questions)
        {
            var given = answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (given is not null && given.ChoiceIndex == question.CorrectIndex)
            {
                score++;
            }
        }

        var total = questions.Count;
        var percent = QuizScoring.Percent(score, total);
        var passed = QuizScoring.Passed(percent, quiz.PassPercent);
        var attempt = new CourseQuizAttempt
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            CourseId = id,
            StudentId = studentId,
            Score = score,
            Total = total,
            Passed = passed,
            AnswersJson = JsonSerializer.Serialize(answers, Json),
            SubmittedAt = DateTimeOffset.UtcNow,
        };
        db.CourseQuizAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new QuizAttemptDto(attempt.Id, score, total, percent, passed, attempt.SubmittedAt));
    }

    internal static List<StoredQuestion> ParseQuestions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<StoredQuestion>>(json, Json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static QuizDetailDto ToDetail(CourseQuiz quiz, bool revealAnswers, int? bestScore, bool? passed)
    {
        var questions = ParseQuestions(quiz.QuestionsJson).Select(q => new QuizQuestionDto(
            q.Id,
            q.Prompt,
            q.Choices.Select((text, index) => new QuizChoiceDto(index, text)).ToList(),
            revealAnswers ? q.CorrectIndex : null)).ToList();
        return new QuizDetailDto(quiz.Id, quiz.Title, quiz.PassPercent, questions, bestScore, passed);
    }

    internal sealed record StoredQuestion(Guid Id, string Prompt, List<string> Choices, int CorrectIndex);
}
