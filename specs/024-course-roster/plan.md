# Implementation Plan: Teacher enrollment roster

**Spec**: `specs/024-course-roster/spec.md`  
**Story**: CH-S24

## Summary

Expose `GET /api/catalog/courses/{id}/roster` for teachers. Catalog checks ownership; Enrollment returns confirmed rows via internal API. Angular roster page linked from editor, gradebook, analytics, and My courses.

## Code to apply

| Area | Path |
|---|---|
| Enrollment internal roster | `EnrollmentEndpoints.cs` |
| Seed confirmed enrollments | `EnrollmentSeeder.cs` |
| Gateway + Catalog endpoint | `EnrollmentGateway.cs`, `RosterEndpoints.cs` |
| UI | `course-roster.ts`, `catalog.api.ts`, routes + nav links |
| Tests | `EnrollmentRosterRules.cs`, `EnrollmentRosterRulesTests.cs` |
