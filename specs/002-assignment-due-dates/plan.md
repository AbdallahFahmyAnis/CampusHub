# Implementation Plan: Assignment due dates and student calendar

**Spec**: `specs/002-assignment-due-dates/spec.md`
**Date**: 2026-08-19

## Summary

Add optional `DueAt` on catalog assignments. Surface it in the teacher editor and student player. Add `GET /api/catalog/calendar` and a My learning calendar list.

## Technical Context

**Language/Version**: .NET 9 Catalog API, Angular learning-mfe + catalog-mfe
**Storage**: SQLite `CourseAssignments.DueAt` TEXT (nullable)
**Edge**: existing `/api/catalog` via gateway
**UI**: course-editor (teacher), course-player Assignments tab, progress-dashboard calendar

## Constitution Check

- [x] Spec exists
- [x] One vertical slice; Catalog owns it
- [x] No new public port
- [x] Seed/schema must not brick Catalog (`ALTER` + try/catch)
- [x] Commit set excludes db/bin/tmp

## Files likely to change (apply)

| Area | Path | Story |
|---|---|---|
| Domain | `src/building-blocks/CampusHub.BuildingBlocks/Sdd/AssignmentDueRules.cs` | CH-S16 |
| Schema | `CatalogSchema.cs` ALTER DueAt | CH-S16 |
| API | `Features/AssignmentEndpoints.cs` list + `GET /calendar` | CH-S16 |
| UI | `course-editor.ts`, `course-player.ts`, `progress-dashboard.ts` | CH-S16 |
| Tests | `tests/CampusHub.Catalog.Api.Tests/AssignmentDueRulesTests.cs` | CH-S16 |
| Mock | `CatalogSeeder.EnsureAssignmentDueDatesAsync` | CH-S16 |

## Test

`dotnet test --filter Story=CH-S16`

Screen: `/learn` calendar shows Linear Algebra due date.

## Research / risks

- SQLite ALTER ADD COLUMN DueAt via TryAsync
- Do not ORDER BY DueAt in SQL (DateTimeOffset); sort in memory
