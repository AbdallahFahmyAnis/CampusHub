using CampusHub.Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
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
            entity.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.Status, x.SubjectId });
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
            entity.HasIndex(x => new { x.SectionId, x.SortOrder });
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
    }
}
