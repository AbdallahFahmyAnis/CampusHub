using CampusHub.Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CampusHub.Catalog.Api.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<CourseReview> CourseReviews => Set<CourseReview>();
    public DbSet<CourseQuestion> CourseQuestions => Set<CourseQuestion>();
    public DbSet<CourseAnswer> CourseAnswers => Set<CourseAnswer>();
    public DbSet<CourseWishlist> CourseWishlists => Set<CourseWishlist>();
    public DbSet<LectureProgress> LectureProgress => Set<LectureProgress>();
    public DbSet<CourseQuiz> CourseQuizzes => Set<CourseQuiz>();
    public DbSet<CourseQuizAttempt> CourseQuizAttempts => Set<CourseQuizAttempt>();
    public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();
    public DbSet<CourseAssignmentSubmission> CourseAssignmentSubmissions => Set<CourseAssignmentSubmission>();
    public DbSet<LectureNote> LectureNotes => Set<LectureNote>();
    public DbSet<CourseAnnouncement> CourseAnnouncements => Set<CourseAnnouncement>();
    public DbSet<CourseResource> CourseResources => Set<CourseResource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasConversion(GuidAsText);
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Subtitle).HasMaxLength(300);
            entity.Property(x => x.Level).HasMaxLength(40);
            entity.Property(x => x.Language).HasMaxLength(40);
            entity.Property(x => x.TeacherId).HasMaxLength(64);
            entity.Property(x => x.TeacherName).HasMaxLength(200);
            entity.Property(x => x.TeacherEmail).HasMaxLength(256);
            entity.Property(x => x.Price).HasPrecision(12, 2);
            entity.Property(x => x.TenantId).HasConversion(GuidAsText);
            entity.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.TenantId, x.Status, x.SubjectId });
        });

        modelBuilder.Entity<CourseSection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasOne(x => x.Course)
                .WithMany(x => x.Sections)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.CourseId, x.SortOrder });
        });

        modelBuilder.Entity<Lecture>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Kind).HasMaxLength(32);
            entity.HasOne(x => x.Section)
                .WithMany(x => x.Lectures)
                .HasForeignKey(x => x.SectionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(x => x.VideoUrl).HasMaxLength(500);
            entity.HasIndex(x => new { x.SectionId, x.SortOrder });
        });

        modelBuilder.Entity<CourseWishlist>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentId).HasMaxLength(64);
            entity.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LectureProgress>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentId).HasMaxLength(64);
            entity.HasIndex(x => new { x.LectureId, x.StudentId }).IsUnique();
            entity.HasIndex(x => new { x.CourseId, x.StudentId });
            entity.HasOne(x => x.Lecture)
                .WithMany()
                .HasForeignKey(x => x.LectureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseReview>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentId).HasMaxLength(64);
            entity.Property(x => x.StudentName).HasMaxLength(200);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
            entity.HasOne(x => x.Course)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseQuestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AuthorId).HasMaxLength(64);
            entity.Property(x => x.AuthorName).HasMaxLength(200);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasOne(x => x.Course)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseAnswer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AuthorId).HasMaxLength(64);
            entity.Property(x => x.AuthorName).HasMaxLength(200);
            entity.HasOne(x => x.Question)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseQuiz>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.CourseId);
        });

        modelBuilder.Entity<CourseQuizAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.QuizId).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.StudentId).HasMaxLength(64);
            entity.HasOne(x => x.Quiz)
                .WithMany()
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.QuizId, x.StudentId, x.SubmittedAt });
        });

        modelBuilder.Entity<CourseAssignment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.CourseId);
        });

        modelBuilder.Entity<CourseAssignmentSubmission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.AssignmentId).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.StudentId).HasMaxLength(64);
            entity.Property(x => x.StudentName).HasMaxLength(200);
            entity.HasOne(x => x.Assignment)
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<LectureNote>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.LectureId).HasConversion(GuidAsText);
            entity.Property(x => x.StudentId).HasMaxLength(64);
            entity.HasIndex(x => new { x.CourseId, x.LectureId, x.StudentId }).IsUnique();
        });

        modelBuilder.Entity<CourseAnnouncement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.AuthorName).HasMaxLength(200);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.CourseId);
        });

        modelBuilder.Entity<CourseResource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.CourseId).HasConversion(GuidAsText);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Url).HasMaxLength(2000);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.CourseId);
        });
    }

    private static readonly ValueConverter<Guid, string> GuidAsText = new(
        id => id.ToString("D"),
        value => ParseGuid(value));

    private static Guid ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
}
