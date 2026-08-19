# Implementation Plan: Course gradebook

**Spec**: `specs/001-course-gradebook/spec.md`  
**Story**: CH-S15  
**Status**: Implemented

## Summary

`GET /gradebook` (teachers) and `GET /grades` (self). Angular gradebook page and player Grades tab. Demo attempts/submissions seeded.

## Technical Context

**Owning service**: Catalog  
## Code to apply

| Area | Path |
|---|---|
| API | `Features/GradeEndpoints.cs` `GET .../gradebook` and `.../grades` |
| UI | `course-gradebook.ts`, player Grades tab |
| Mock | Seeded quiz attempts + assignment scores |

## Test

Teacher roster vs student self row. Algorithms + Linear Algebra.
