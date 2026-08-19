namespace CampusHub.Catalog.Api.Contracts;

public sealed record SubjectDto(Guid Id, string Code, string Name, string? Description);

public sealed record PagedCoursesDto(
    IReadOnlyList<CourseListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CourseListItemDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string SubjectCode,
    string SubjectName,
    string TeacherName,
    int Capacity,
    int RemainingSeats,
    decimal Price,
    string Status,
    string? Level,
    double RatingAverage,
    int RatingCount,
    int LectureCount,
    int DurationMinutes,
    bool Wishlisted);

public sealed record CourseDetailDto(
    Guid Id,
    Guid SubjectId,
    string Title,
    string? Subtitle,
    string? Description,
    string SubjectCode,
    string SubjectName,
    string TeacherId,
    string TeacherName,
    string TeacherEmail,
    int Capacity,
    int RemainingSeats,
    decimal Price,
    string Status,
    bool CanEnroll,
    bool Enrolled,
    string? Level,
    string? Language,
    IReadOnlyList<string> Outcomes,
    IReadOnlyList<string> Requirements,
    double RatingAverage,
    int RatingCount,
    int LectureCount,
    int DurationMinutes,
    bool Wishlisted);

public sealed record AskCourseRequest(string Question, Guid? LectureId);

public sealed record AskCourseResponse(string Answer, string Source);

public sealed record CatalogCapabilitiesDto(string Search, string Tutor);

public sealed record CreateSubjectRequest(string Code, string Name, string? Description);

public sealed record CreateCourseRequest(
    Guid SubjectId,
    string Title,
    string? Description,
    int Capacity,
    decimal Price,
    string? Subtitle,
    string? Level,
    string? Language,
    string? Outcomes,
    string? Requirements);

public sealed record UpdateCourseRequest(
    Guid SubjectId,
    string Title,
    string? Description,
    int Capacity,
    decimal Price,
    string? Subtitle,
    string? Level,
    string? Language,
    string? Outcomes,
    string? Requirements);

public sealed record LectureOutlineDto(
    Guid Id,
    string Title,
    string Kind,
    int DurationMinutes,
    string? Summary,
    bool IsPreview,
    int SortOrder,
    string? VideoUrl,
    bool Completed);

public sealed record SectionDto(
    Guid Id,
    string Title,
    int SortOrder,
    IReadOnlyList<LectureOutlineDto> Lectures);

public sealed record CurriculumDto(Guid CourseId, IReadOnlyList<SectionDto> Sections);

public sealed record LectureDetailDto(
    Guid Id,
    Guid SectionId,
    Guid CourseId,
    string Title,
    string Kind,
    int DurationMinutes,
    string? Summary,
    string? Body,
    bool IsPreview,
    bool Locked,
    int SortOrder,
    string? VideoUrl,
    bool Completed);

public sealed record CreateSectionRequest(string Title);

public sealed record CreateLectureRequest(
    string Title,
    string? Kind,
    int DurationMinutes,
    string? Summary,
    string? Body,
    bool IsPreview,
    string? VideoUrl);

public sealed record ReviewDto(
    Guid Id,
    string StudentName,
    int Rating,
    string? Title,
    string Body,
    DateTimeOffset CreatedAt,
    bool Mine);

public sealed record CreateReviewRequest(int Rating, string? Title, string Body);

public sealed record AnswerDto(
    Guid Id,
    string AuthorName,
    string Body,
    bool IsTeacher,
    DateTimeOffset CreatedAt);

public sealed record QuestionDto(
    Guid Id,
    string AuthorName,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AnswerDto> Answers);

public sealed record CreateQuestionRequest(string Title, string Body);

public sealed record CreateAnswerRequest(string Body);

public sealed record CourseProgressDto(
    Guid CourseId,
    string CourseTitle,
    string SubjectCode,
    int TotalLectures,
    int CompletedLectures,
    int ProgressPct,
    DateTimeOffset? LastActivityAt,
    Guid? ContinueLectureId,
    int QuizCount,
    int QuizzesPassed,
    int? BestQuizPercent,
    int AssignmentCount,
    int AssignmentsSubmitted,
    int NotesCount);

public sealed record StudentProgressDashboardDto(
    IReadOnlyList<CourseProgressDto> Courses,
    int StreakDays,
    int TotalLecturesCompleted,
    DateTimeOffset? LastActivityAt);

public sealed record QuizChoiceDto(int Index, string Text);

public sealed record QuizQuestionDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<QuizChoiceDto> Choices,
    int? CorrectIndex);

public sealed record QuizSummaryDto(
    Guid Id,
    string Title,
    int PassPercent,
    int QuestionCount,
    int? BestScore,
    bool? Passed);

public sealed record QuizDetailDto(
    Guid Id,
    string Title,
    int PassPercent,
    IReadOnlyList<QuizQuestionDto> Questions,
    int? BestScore,
    bool? Passed);

public sealed record CreateQuizQuestionRequest(string Prompt, IReadOnlyList<string> Choices, int CorrectIndex);

public sealed record CreateQuizRequest(string Title, int PassPercent, IReadOnlyList<CreateQuizQuestionRequest> Questions);

public sealed record SubmitQuizAnswerRequest(Guid QuestionId, int ChoiceIndex);

public sealed record SubmitQuizRequest(IReadOnlyList<SubmitQuizAnswerRequest> Answers);

public sealed record QuizAttemptDto(
    Guid Id,
    int Score,
    int Total,
    int Percent,
    bool Passed,
    DateTimeOffset SubmittedAt);

public sealed record AssignmentSummaryDto(
    Guid Id,
    string Title,
    string Instructions,
    int MaxScore,
    bool Submitted,
    int? Score,
    string? Feedback,
    int SubmissionCount);

public sealed record CreateAssignmentRequest(string Title, string Instructions, int MaxScore);

public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    string AuthorName,
    DateTimeOffset CreatedAt);

public sealed record CreateAnnouncementRequest(string Title, string Body);

public sealed record GradebookColumnDto(string Kind, Guid Id, string Title, int MaxScore);

public sealed record GradebookCellDto(Guid ItemId, int? Score, int MaxScore, bool Submitted);

public sealed record GradebookRowDto(
    string StudentId,
    string StudentName,
    IReadOnlyList<GradebookCellDto> Cells,
    double? Percent);

public sealed record GradebookDto(
    Guid CourseId,
    string CourseTitle,
    IReadOnlyList<GradebookColumnDto> Columns,
    IReadOnlyList<GradebookRowDto> Rows);

public sealed record SubmitAssignmentRequest(string Body);

public sealed record AssignmentSubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string StudentId,
    string StudentName,
    string Body,
    int? Score,
    string? Feedback,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? GradedAt);

public sealed record GradeAssignmentRequest(int Score, string? Feedback);

public sealed record LectureStatDto(
    Guid Id,
    string Title,
    string SectionTitle,
    int DurationMinutes,
    int CompletionCount);

public sealed record MonthlyEnrollmentDto(string Month, int Count, decimal Revenue);

public sealed record CourseStatsDto(
    Guid CourseId,
    string CourseTitle,
    int TotalLectures,
    int StudentsCompletedAll,
    double AverageRating,
    int ReviewCount,
    IReadOnlyList<LectureStatDto> LectureStats,
    int TotalEnrollments,
    int ConfirmedEnrollments,
    int CancelledEnrollments,
    decimal TotalRevenue,
    IReadOnlyList<MonthlyEnrollmentDto> MonthlyBreakdown);

public sealed record LectureNoteDto(
    Guid CourseId,
    Guid LectureId,
    string Body,
    DateTimeOffset? UpdatedAt);

public sealed record SaveLectureNoteRequest(string Body);

public sealed record LectureNoteListItemDto(
    Guid CourseId,
    string CourseTitle,
    Guid LectureId,
    string LectureTitle,
    string Snippet,
    DateTimeOffset UpdatedAt);
