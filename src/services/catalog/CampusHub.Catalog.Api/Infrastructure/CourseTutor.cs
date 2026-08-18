using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using CampusHub.Catalog.Api.Domain;

namespace CampusHub.Catalog.Api.Infrastructure;

public sealed class CourseTutor(HttpClient http, IConfiguration config, ILogger<CourseTutor> logger)
{
    public bool ModelEnabled =>
        !string.IsNullOrWhiteSpace(config["Ai:ApiKey"]) &&
        !string.IsNullOrWhiteSpace(config["Ai:BaseUrl"]);

    public async Task<string> AnswerAsync(string question, Course course, Lecture? lecture, bool allowModel, CancellationToken ct)
    {
        var materials = BuildMaterials(course, lecture);
        if (ModelEnabled && allowModel)
        {
            try
            {
                var modelAnswer = await AskModelAsync(question, materials, ct);
                if (!string.IsNullOrWhiteSpace(modelAnswer))
                {
                    return modelAnswer.Trim();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI tutor request failed; using catalog text.");
            }
        }

        return FromCatalog(question, materials, course.Title);
    }

    private async Task<string?> AskModelAsync(string question, string materials, CancellationToken ct)
    {
        var key = config["Ai:ApiKey"]!;
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = JsonContent.Create(new
        {
            model = config["Ai:Model"] ?? "gpt-4o-mini",
            temperature = 0.2,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                        You are the CampusHub course tutor. Answer only from the supplied course materials.
                        If the materials do not contain the answer, say so and point the student to the lecture or Q&A.
                        Keep answers under 180 words. Do not invent APIs, grades, or campus policy.
                        """
                },
                new { role = "user", content = $"Course materials:\n{materials}\n\nStudent question:\n{question}" }
            }
        });
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<ChatCompletion>(ct);
        return body?.Choices?.FirstOrDefault()?.Message?.Content;
    }

    private static string FromCatalog(string question, string materials, string title)
    {
        var terms = question
            .Split([' ', '?', '.', ',', '!', ';', ':', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 2)
            .Select(term => term.ToLowerInvariant())
            .Distinct()
            .ToList();
        var sentences = materials
            .Split(['\n', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(sentence => sentence.Length > 24)
            .ToList();
        var hits = sentences
            .Select(sentence => (sentence, score: terms.Count(term => sentence.Contains(term, StringComparison.OrdinalIgnoreCase))))
            .Where(item => item.score > 0)
            .OrderByDescending(item => item.score)
            .Take(3)
            .Select(item => item.sentence.Trim())
            .ToList();
        if (hits.Count == 0)
        {
            return $"I can only answer from {title}. Try asking about an outcome, a definition, or a step from this lecture. If you are enrolled, the Q&A tab reaches the instructor.";
        }

        return string.Join(" ", hits) + " (From this course’s materials. Connect an AI key to get a fuller explanation.)";
    }

    private static string BuildMaterials(Course course, Lecture? lecture)
    {
        var text = new StringBuilder();
        text.AppendLine($"Title: {course.Title}");
        if (!string.IsNullOrWhiteSpace(course.Subtitle))
        {
            text.AppendLine(course.Subtitle);
        }

        if (!string.IsNullOrWhiteSpace(course.Description))
        {
            text.AppendLine(course.Description);
        }

        if (!string.IsNullOrWhiteSpace(course.Outcomes))
        {
            text.AppendLine("Outcomes:");
            text.AppendLine(course.Outcomes);
        }

        if (lecture is not null)
        {
            text.AppendLine($"Lecture: {lecture.Title}");
            if (!string.IsNullOrWhiteSpace(lecture.Summary))
            {
                text.AppendLine(lecture.Summary);
            }

            if (!string.IsNullOrWhiteSpace(lecture.Body))
            {
                var body = lecture.Body.Length > 5000 ? lecture.Body[..5000] : lecture.Body;
                text.AppendLine(body);
            }
        }

        return text.ToString();
    }

    private sealed class ChatCompletion
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
