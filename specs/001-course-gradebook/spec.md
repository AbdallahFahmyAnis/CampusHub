# Feature Specification: Course gradebook

**Story**: CH-S15
**Feature Branch**: `main`
**Created**: 2026-08-19
**Status**: Implemented
**Input**: Teachers need one roster of quiz and assignment scores; students need a Grades tab.

## User Scenarios & Testing

### User Story 1 - Teacher gradebook (Priority: P1)

A teacher opens `/catalog/:id/gradebook` and sees students who submitted quizzes or assignments, with an overall percent.

**Independent Test**: Sign in as `teacher@campushub.local`, open Algorithms and Linear Algebra gradebooks.

**Acceptance Scenarios**:

1. **Given** seeded quiz attempts on Algorithms, **When** the teacher opens the gradebook, **Then** Sam and Noah show quiz percents.
2. **Given** seeded assignment submissions on Linear Algebra, **When** the teacher opens the gradebook, **Then** Sam shows 88/100 and Noah shows ungraded.

### User Story 2 - Student grades (Priority: P1)

An enrolled student opens the course player Grades tab and sees only their row.

**Independent Test**: Sign in as `student@campushub.local`, open an enrolled course player → Grades.

## Requirements

- **FR-001**: `GET /api/catalog/courses/{id}/gradebook` MUST be teacher/admin (catalog manage + owner check).
- **FR-002**: `GET /api/catalog/courses/{id}/grades` MUST return the caller's row.
- **FR-003**: Quiz cells are best-attempt percent (max 100). Assignment cells use points and may be ungraded.
- **FR-004**: Catalog seed MUST NOT crash if demo grade rows cannot be inserted.

## Success Criteria

- **SC-001**: Teacher can open gradebook from My courses, editor, and analytics.
- **SC-002**: Student can read grades without seeing other students' scores.

## Assumptions

- Roster is inferred from quiz attempts and assignment submissions, not the Enrollment service.
