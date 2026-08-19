namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>SDD CH-S16 / MDP-27 — specs/002-assignment-due-dates. Overdue and late flags for assignments.</summary>
public static class AssignmentDueRules
{
    public static bool Overdue(DateTimeOffset? dueAt, bool submitted, DateTimeOffset? now = null)
    {
        var at = now ?? DateTimeOffset.UtcNow;
        return dueAt is not null && !submitted && at > dueAt;
    }

    public static bool Late(DateTimeOffset? dueAt, DateTimeOffset? submittedAt) =>
        dueAt is not null && submittedAt is not null && submittedAt > dueAt;
}
