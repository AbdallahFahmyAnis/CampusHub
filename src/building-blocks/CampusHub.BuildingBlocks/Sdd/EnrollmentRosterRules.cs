namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>SDD CH-S24 — specs/024-course-roster. Order roster rows for display.</summary>
public static class EnrollmentRosterRules
{
    public static IReadOnlyList<T> OrderByEnrolledAt<T>(
        IEnumerable<T> rows,
        Func<T, DateTimeOffset> enrolledAt,
        Func<T, string> tieBreaker)
    {
        return rows
            .OrderBy(enrolledAt)
            .ThenBy(tieBreaker, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
