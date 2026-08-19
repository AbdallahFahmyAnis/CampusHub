# Feature Specification: Course player and curriculum

**Story**: CH-S20  
**Workflow**: Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done**  
**Created**: 2026-08-20  
**Status**: Implemented

## Qualified header

| Field | Value |
|---|---|
| Persona | Enrolled student or staff |
| Screens | `/learn/course/:courseId`, `/learn/course/:courseId/:lectureId` |
| Code | `course-player.ts`, `CourseLearningEndpoints.cs` |

## A–Z

1. Login (CH-S17). Enroll if student (CH-S19).
2. Open the player (from catalog “Go to course” or `/learn` Continue).
3. Lecture tab: watch/read, mark complete (unlocks next). Preview lectures work before enroll.
4. Other tabs are their own stories but live **on this screen**: Notes CH-S13, Ask AI CH-S08, Quiz CH-S11, Assignments CH-S12, Announcements CH-S14, Grades CH-S15, Q&A/reviews on the player + course landing.
5. Completing **all** lectures emits CourseCompleted → certificate (CH-S06).
6. Session: Bearer JWT via BFF; unenrolled student is forbidden on full lecture body / complete / ask / quiz submit (staff bypass).

## Success Criteria

- **SC-001**: Enrolled student can open Algorithms player and complete a lecture.
