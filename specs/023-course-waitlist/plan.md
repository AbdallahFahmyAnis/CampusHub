# Implementation Plan: Course waitlist

**Spec**: `specs/023-course-waitlist/spec.md`  
**Story**: CH-S23  
**Workflow stage after this plan**: Apply → Test → Mock → Done

## Summary

Enrollment-owned waitlist queue for full published courses. Students join/leave from course detail; list with position on My enrollments. Catalog seeds Distributed Systems with zero remaining seats. Confirmed enroll clears the waitlist row.

## Technical Context

**Language/Version**: .NET 9 / Angular  
**Edge**: `http://localhost:5000`  
**Owning service**: Enrollment (seat counts stay in Catalog)

## Constitution Check

- [x] Spec exists with screens + AC
- [x] One vertical slice
- [x] No new public port
- [x] Story id in new types/endpoints

## Code to apply *(mandatory)*

| Area | Path | Story |
|---|---|---|
| Domain + DB | `CourseWaitlist`, `EnrollmentDbContext`, `EnrollmentSchema` | CH-S23 |
| API | `WaitlistEndpoints.cs`, map from Program | CH-S23 |
| Saga | Clear waitlist on confirm | CH-S23 |
| Rules + tests | `WaitlistRules.cs`, tests | CH-S23 |
| Mock | Catalog full Distributed; Enrollment seed queue | CH-S23 |
| UI | `course-detail.ts`, `enrollment.api.ts`, `my-enrollments.ts` | CH-S23 |
| Traceability | `SddStories`, STORIES, product backlog | CH-S23 |
