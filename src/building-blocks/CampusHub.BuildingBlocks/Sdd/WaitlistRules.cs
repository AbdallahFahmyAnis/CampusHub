namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>SDD CH-S23 — specs/023-course-waitlist. Queue position helpers.</summary>
public static class WaitlistRules
{
    /// <summary>1-based position among entries ordered by CreatedAt ascending (stable Guid tie-break).</summary>
    public static int Position(
        IReadOnlyList<(Guid Id, DateTimeOffset CreatedAt)> queue,
        Guid entryId)
    {
        var ordered = queue
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select((x, i) => (x.Id, Index: i + 1))
            .ToList();
        var hit = ordered.FirstOrDefault(x => x.Id == entryId);
        return hit.Id == entryId ? hit.Index : 0;
    }

    public static bool CanJoin(bool published, int remainingSeats, bool alreadyEnrolled, bool alreadyWaitlisted) =>
        published && remainingSeats <= 0 && !alreadyEnrolled && !alreadyWaitlisted;
}
