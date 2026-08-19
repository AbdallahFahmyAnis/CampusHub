# Feature Specification: Assignment due dates and student calendar

**Feature Branch**: `cursor/sdd-spec-driven-development`
**Created**: 2026-08-19
**Status**: Implemented
**Input**: Teachers set a due date on assignments; students see upcoming and overdue work on My learning.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Teacher sets a due date (Priority: P1)

A teacher adding or viewing an assignment can set an optional due date. Students see that date on the assignment in the course player.

**Why this priority**: Without a due date, assignments have no schedule.

**Independent Test**: Edit Linear Algebra, add or inspect the span-and-basis assignment due date; open the player Assignments tab as a student.

**Acceptance Scenarios**:

1. **Given** a teacher on the course editor, **When** they create an assignment with a due date, **Then** students see that date on the assignment.
2. **Given** an assignment with no due date, **When** a student opens Assignments, **Then** the work is still submittable and no due label is required.
3. **Given** a due date in the past and no submission, **When** a student views the assignment, **Then** it is marked overdue. Submitting after the due date is still allowed and marked late.

---

### User Story 2 - Student calendar on My learning (Priority: P1)

A student opens My learning and sees a calendar (upcoming list by date) of assignment due dates for courses they are already learning.

**Why this priority**: Due dates are useless if they only live inside each course.

**Independent Test**: Sign in as `student@campushub.local`, open `/learn`, confirm the Linear Algebra write-up appears with a due date.

**Acceptance Scenarios**:

1. **Given** an enrolled/learning student with at least one dated assignment, **When** they open My learning, **Then** they see it grouped by due date with a link into the course.
2. **Given** an overdue unsubmitted assignment, **When** they open the calendar, **Then** it is visually distinct from upcoming items.

### Edge Cases

- Assignments without due dates do not appear on the calendar.
- Catalog must start if the DueAt column is missing until schema ensure adds it.
- Calendar is empty when the student has no dated work in courses they have progress or submissions in.

## Requirements *(mandatory)*

- **FR-001**: Teachers MUST be able to set an optional due date when creating an assignment.
- **FR-002**: Students MUST see due date, overdue, and late status on the course assignment list.
- **FR-003**: Students MUST see dated assignments on My learning, ordered by due date.
- **FR-004**: Submitting after the due date MUST remain allowed and MUST be marked late.
- **FR-005**: Seeded Linear Algebra assignment MUST have a due date so the calendar is demoable.

### Key Entities

- **Assignment**: existing work item; optional due instant.
- **Calendar item**: assignment due for the current student, with submitted/overdue/late flags.

## Success Criteria *(mandatory)*

- **SC-001**: Teacher can save a due date from the course editor.
- **SC-002**: Student can find the dated assignment on My learning without opening the course first.
- **SC-003**: Overdue and late states are visible in the player.

## Assumptions

- Calendar uses Catalog data (assignments + the student's lecture progress and submissions), not a new Enrollment roster API.
- Out of scope: quiz due dates, timezone picker, blocking late submits, email reminders.
