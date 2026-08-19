# Implementation Plan: Course quizzes

**Spec**: `specs/013-quizzes/spec.md`  
**Story**: CH-S11 / MDP-22  
**Workflow**: Specify ✅ → Apply ✅ → Test ✅ → Mock ✅ → **Done**

## Summary

Teachers author multiple-choice quizzes; students attempt them in the player. Scoring lives in `QuizScoring` so tests can lock CH-S11 without HTTP.

## Code to apply

| Area | Path |
|---|---|
| API | `src/services/catalog/CampusHub.Catalog.Api/Features/QuizEndpoints.cs` |
| Domain | `src/building-blocks/CampusHub.BuildingBlocks/Sdd/QuizScoring.cs` |
| Schema | `CatalogSchema.cs` CourseQuizzes / CourseQuizAttempts (TEXT ids, no brittle FK) |
| Seed | `CatalogSeeder.cs` Algorithms checkpoint |
| UI teacher | `src/frontend/projects/catalog-mfe/src/app/course-editor.ts` |
| UI student | `src/frontend/projects/learning-mfe/src/app/course-player.ts` Quiz tab |
| Tests | `tests/CampusHub.Catalog.Api.Tests/QuizScoringTests.cs` `[Trait("Story","CH-S11")]` |

## Test

`dotnet test --filter Story=CH-S11`

Screen: teacher@ edits Algorithms; student@ Quiz tab → submit → percent.

## Mock

Seeded Algorithms quiz pass percent 70.
