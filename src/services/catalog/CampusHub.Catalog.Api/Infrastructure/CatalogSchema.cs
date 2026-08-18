using Microsoft.EntityFrameworkCore;

namespace CampusHub.Catalog.Api.Infrastructure;

internal static class CatalogSchema
{
    public static async Task EnsureAsync(CatalogDbContext db, CancellationToken ct)
    {
        await db.Database.EnsureCreatedAsync(ct);
        await TryAsync(db, "ALTER TABLE Courses ADD COLUMN Subtitle TEXT", ct);
        await TryAsync(db, "ALTER TABLE Courses ADD COLUMN Level TEXT", ct);
        await TryAsync(db, "ALTER TABLE Courses ADD COLUMN Language TEXT", ct);
        await TryAsync(db, "ALTER TABLE Courses ADD COLUMN Outcomes TEXT", ct);
        await TryAsync(db, "ALTER TABLE Courses ADD COLUMN Requirements TEXT", ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS CourseSections (
                Id TEXT NOT NULL CONSTRAINT PK_CourseSections PRIMARY KEY,
                CourseId TEXT NOT NULL,
                Title TEXT NOT NULL,
                SortOrder INTEGER NOT NULL,
                CONSTRAINT FK_CourseSections_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES Courses (Id) ON DELETE CASCADE
            );
            """, ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS Lectures (
                Id TEXT NOT NULL CONSTRAINT PK_Lectures PRIMARY KEY,
                SectionId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Kind TEXT NOT NULL,
                DurationMinutes INTEGER NOT NULL,
                Summary TEXT NULL,
                Body TEXT NULL,
                IsPreview INTEGER NOT NULL,
                SortOrder INTEGER NOT NULL,
                CONSTRAINT FK_Lectures_CourseSections_SectionId FOREIGN KEY (SectionId) REFERENCES CourseSections (Id) ON DELETE CASCADE
            );
            """, ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS CourseReviews (
                Id TEXT NOT NULL CONSTRAINT PK_CourseReviews PRIMARY KEY,
                CourseId TEXT NOT NULL,
                StudentId TEXT NOT NULL,
                StudentName TEXT NOT NULL,
                Rating INTEGER NOT NULL,
                Title TEXT NULL,
                Body TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                CONSTRAINT FK_CourseReviews_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES Courses (Id) ON DELETE CASCADE
            );
            """, ct);
        await TryAsync(db, """
            CREATE UNIQUE INDEX IF NOT EXISTS IX_CourseReviews_CourseId_StudentId ON CourseReviews (CourseId, StudentId);
            """, ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS CourseQuestions (
                Id TEXT NOT NULL CONSTRAINT PK_CourseQuestions PRIMARY KEY,
                CourseId TEXT NOT NULL,
                AuthorId TEXT NOT NULL,
                AuthorName TEXT NOT NULL,
                Title TEXT NOT NULL,
                Body TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                CONSTRAINT FK_CourseQuestions_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES Courses (Id) ON DELETE CASCADE
            );
            """, ct);
        await TryAsync(db, """
            CREATE TABLE IF NOT EXISTS CourseAnswers (
                Id TEXT NOT NULL CONSTRAINT PK_CourseAnswers PRIMARY KEY,
                QuestionId TEXT NOT NULL,
                AuthorId TEXT NOT NULL,
                AuthorName TEXT NOT NULL,
                Body TEXT NOT NULL,
                IsTeacher INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                CONSTRAINT FK_CourseAnswers_CourseQuestions_QuestionId FOREIGN KEY (QuestionId) REFERENCES CourseQuestions (Id) ON DELETE CASCADE
            );
            """, ct);
    }

    private static async Task TryAsync(CatalogDbContext db, string sql, CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, ct);
        }
        catch
        {
            // Column or table already exists on an older SQLite catalog.db.
        }
    }
}
