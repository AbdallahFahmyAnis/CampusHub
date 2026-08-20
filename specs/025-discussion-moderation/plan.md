# Implementation Plan: Discussion pinning and Q&A moderation

**Spec**: `specs/025-discussion-moderation/spec.md`  
**Story**: CH-S25

## Summary

Add `IsPinned` / `IsHidden` on questions and `IsHidden` on answers. Teacher-only POST endpoints for pin and hide. List API sorts pinned first and filters hidden content for students. UI on editor, detail, and player.

## Code to apply

| Area | Path |
|---|---|
| Schema + entities | `CatalogSchema.cs`, `Entities.cs`, `CatalogDbContext.cs` |
| DTOs + mapping | `Dtos.cs`, `CatalogMappings.cs` |
| API | `CourseLearningEndpoints.cs` |
| Rules + tests | `QuestionModerationRules.cs`, `QuestionModerationRulesTests.cs` |
| Seed | `CatalogSeeder.cs` |
| UI | `catalog.api.ts`, `course-detail.ts`, `course-player.ts`, `course-editor.ts` |
