# Feature Specification: Course assignments

**Story**: CH-S12  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Submit and grade written work (Priority: P1)

Teachers create assignments. Students submit text. Teachers grade with score and feedback.

**Independent Test**: Linear Algebra write-up as student; grade as teacher.

**Acceptance Scenarios**:

1. **Given** an enrolled student, **When** they submit, **Then** the teacher sees the submission.
2. **Given** a teacher, **When** they grade, **Then** the student sees score and feedback.

## Requirements

- **FR-001**: One submission per student per assignment (resubmit allowed).
- **FR-002**: Grading MUST clamp to max score.

## Success Criteria

- **SC-001**: Assignments tab and editor grading panel both work.

## Qualified story

| Field | Value |
|---|---|
| **Jira idea** | [MDP-23](https://abdallah-fahmy.atlassian.net/browse/MDP-23) |
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done** |

### Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| Editor assignments | `http://localhost:5000/catalog/{id}/edit` | teacher | Create + grade submissions |
| Player Assignments | `http://localhost:5000/learn/course/{id}` | student | Submit text, see score/feedback |

Linear Algebra: `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2`.

### Apply (code)

- `AssignmentEndpoints.cs` (CH-S12; due dates CH-S16)
- `course-editor.ts`, `course-player.ts`

### Test / Mock

- Student submits Linear Algebra write-up; teacher grades; clamp to max score. Seeded assignment `dddddddd-dddd-dddd-dddd-dddddddddd00`.
