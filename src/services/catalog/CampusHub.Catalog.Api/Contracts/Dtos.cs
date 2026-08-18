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
