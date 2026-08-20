namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>SDD CH-S25 — specs/025-discussion-moderation. Pin/hide rules for course Q&amp;A.</summary>
public static class QuestionModerationRules
{
    public static bool CanModerate(bool canManageCatalog, bool isCourseOwner, bool isAdministrator) =>
        isAdministrator || (canManageCatalog && isCourseOwner);

    public static IEnumerable<T> OrderForDisplay<T>(
        IEnumerable<T> questions,
        Func<T, bool> isPinned,
        Func<T, DateTimeOffset> createdAt) =>
        questions.OrderByDescending(isPinned).ThenByDescending(createdAt);

    public static IEnumerable<T> VisibleToStudents<T>(IEnumerable<T> questions, Func<T, bool> isHidden) =>
        questions.Where(q => !isHidden(q));
}
