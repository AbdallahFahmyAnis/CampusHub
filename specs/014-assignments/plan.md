# Implementation Plan: Course assignments

**Spec**: `specs/014-assignments/spec.md`  
**Story**: CH-S12 / MDP-23  
**Workflow**: Specify ✅ → Apply ✅ → Test ✅ → Mock ✅ → **Done**

## Summary

Written assignments: create, submit, grade. Due dates are CH-S16 on the same endpoints.

## Code to apply

| Area | Path |
|---|---|
| API | `Features/AssignmentEndpoints.cs` |
| DTOs | `Contracts/Dtos.cs` Assignment* |
| Schema | `CourseAssignments`, `CourseAssignmentSubmissions` |
| UI | `course-editor.ts` grading, `course-player.ts` Assignments tab |
| Mock | Linear Algebra write-up id `dddddddd-dddd-dddd-dddd-dddddddddd00` |

## Test

Student submit → teacher grade clamped to max. Screen smoke on Linear Algebra.
