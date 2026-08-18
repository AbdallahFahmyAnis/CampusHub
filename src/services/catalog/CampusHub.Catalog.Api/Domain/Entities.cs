namespace CampusHub.Catalog.Api.Domain;

public sealed class Subject
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public enum CourseStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public sealed class Course
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string TeacherId { get; set; }
    public required string TeacherName { get; set; }
    public required string TeacherEmail { get; set; }
    public int Capacity { get; set; }
    public int RemainingSeats { get; set; }
    public decimal Price { get; set; }
    public CourseStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? Subtitle { get; set; }
    public string? Level { get; set; }
    public string? Language { get; set; }
    public string? Outcomes { get; set; }
    public string? Requirements { get; set; }

    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
    public ICollection<CourseReview> Reviews { get; set; } = new List<CourseReview>();
    public ICollection<CourseQuestion> Questions { get; set; } = new List<CourseQuestion>();

    public bool CanEnroll => Status == CourseStatus.Published && RemainingSeats > 0;

    public void Publish()
    {
        if (Capacity < 1)
        {
            throw new InvalidOperationException("A course needs capacity before it can be published.");
        }

        Status = CourseStatus.Published;
        PublishedAt ??= DateTimeOffset.UtcNow;
        if (RemainingSeats == 0 && Status == CourseStatus.Published)
        {
            RemainingSeats = Capacity;
        }
    }

    public bool TryReserveSeat()
    {
        if (!CanEnroll)
        {
            return false;
        }

        RemainingSeats--;
        return true;
    }

    public void ReleaseSeat()
    {
        if (RemainingSeats < Capacity)
        {
            RemainingSeats++;
        }
    }
}

public sealed class CourseSection
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public required string Title { get; set; }
    public int SortOrder { get; set; }
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
}

public sealed class Lecture
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public CourseSection Section { get; set; } = null!;
    public required string Title { get; set; }
    public required string Kind { get; set; }
    public int DurationMinutes { get; set; }
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public bool IsPreview { get; set; }
    public int SortOrder { get; set; }
}

public sealed class CourseReview
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public required string StudentId { get; set; }
    public required string StudentName { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CourseQuestion
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public required string AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<CourseAnswer> Answers { get; set; } = new List<CourseAnswer>();
}

public sealed class CourseAnswer
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public CourseQuestion Question { get; set; } = null!;
    public required string AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required string Body { get; set; }
    public bool IsTeacher { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
