# Implementation Plan: Course resources

**Spec**: `specs/022-course-resources/spec.md`  
**Story**: CH-S22  
**Workflow stage after this plan**: Apply → Test → Mock → Done

## Summary

Add Catalog-owned course resource links (title, https URL, optional description). Teachers author them in the course editor; students read them on a new player Resources tab. Mirror announcements (CH-S14).

## Technical Context

**Language/Version**: .NET 9 / Angular  
**Edge**: `http://localhost:5000`  
**Owning service**: Catalog  
**UI**: catalog-mfe editor + learning-mfe player

## Constitution Check

- [x] Spec exists with screens + AC
- [x] One vertical slice (Catalog + existing MFEs)
- [x] No new public port
- [x] Story id in new types/endpoints

## Code to apply *(mandatory)*

| Area | Path | Story |
|---|---|---|
| Domain + schema | `Entities.cs`, `CatalogDbContext.cs`, `CatalogSchema.cs` | CH-S22 |
| API | `Features/ResourceEndpoints.cs`, `CatalogEndpoints.cs`, `Dtos.cs` | CH-S22 |
| Rules + tests | `CourseResourceRules.cs`, `tests/.../CourseResourceRulesTests.cs` | CH-S22 |
| Mock | `CatalogSeeder.cs` | CH-S22 |
| UI | `catalog.api.ts`, `course-editor.ts`, `course-player.ts`, `sdd-stories.ts` | CH-S22 |
| Traceability | `SddStories.cs` | CH-S22 |
