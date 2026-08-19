namespace CampusHub.BuildingBlocks.Sdd;

/// <summary>SDD CH-S11 / MDP-22 — specs/013-quizzes. Percent and pass/fail for a quiz attempt.</summary>
public static class QuizScoring
{
    public static int Percent(int score, int total) =>
        total <= 0 ? 0 : (int)Math.Round(score * 100.0 / total);

    public static bool Passed(int percent, int passPercent) => percent >= passPercent;
}
