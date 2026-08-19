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

## Files likely to change

- `src/services/catalog/CampusHub.Catalog.Api/Domain/Entities.cs`
- `src/services/catalog/CampusHub.Catalog.Api/Infrastructure/CatalogSchema.cs`
- `src/services/catalog/CampusHub.Catalog.Api/Infrastructure/CatalogDbContext.cs`
- `src/services/catalog/CampusHub.Catalog.Api/Infrastructure/CatalogSeeder.cs`
- `src/services/catalog/CampusHub.Catalog.Api/Contracts/Dtos.cs`
- `src/services/catalog/CampusHub.Catalog.Api/Features/AssignmentEndpoints.cs`
- `src/services/catalog/CampusHub.Catalog.Api/Features/CatalogEndpoints.cs`
- `src/frontend/projects/catalog-mfe/src/app/catalog.api.ts`
- `src/frontend/projects/catalog-mfe/src/app/course-editor.ts`
- `src/frontend/projects/learning-mfe/src/app/course-player.ts`
- `src/frontend/projects/learning-mfe/src/app/progress-dashboard.ts`

## Research / risks

- SQLite ALTER ADD COLUMN DueAt via TryAsync
- Do not ORDER BY DueAt in SQL (DateTimeOffset); sort in memory
