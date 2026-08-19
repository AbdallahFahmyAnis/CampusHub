# Implementation Plan: [FEATURE]

**Spec**: `specs/NNN-slug/spec.md`  
**Story**: CH-Snn / MDP-nn  
**Workflow stage after this plan**: Apply → Test → Mock → Done

## Summary

[One paragraph]

## Technical Context

**Language/Version**: .NET 9 / Angular  
**Edge**: `http://localhost:5000`  
**Owning service**: [Catalog | Identity | …]

## Constitution Check

- [ ] Spec exists with screens + AC
- [ ] One vertical slice
- [ ] No new public port
- [ ] Story id in new types/endpoints

## Code to apply *(mandatory)*

| Area | Path | Story |
|---|---|---|
| API | `src/services/.../Features/*.cs` | CH-Snn |
| UI | `src/frontend/projects/...` | CH-Snn |
| Tests | `tests/CampusHub.Catalog.Api.Tests/` | `[Trait("Story", "CH-Snn")]` |
| Mock | seeder / schema | CH-Snn |
