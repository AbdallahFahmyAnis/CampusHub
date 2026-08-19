# Feature Specification: Course quizzes

**Story**: CH-S11  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Author and take a quiz (Priority: P1)

Teachers add multiple-choice quizzes. Students take them in the player and keep a best score.

**Independent Test**: Teacher adds a quiz on Algorithms; student starts and submits.

**Acceptance Scenarios**:

1. **Given** a teacher, **When** they add a quiz, **Then** it appears on the player Quiz tab.
2. **Given** an enrolled student, **When** they submit, **Then** they see percent and pass/fail.
3. **Given** a missing attempts table, **When** the quiz list loads, **Then** it must not 500.

## Requirements

- **FR-001**: Attempts MUST be per student.
- **FR-002**: Schema ensure MUST create attempts without a brittle FK.

## Success Criteria

- **SC-001**: Seeded Algorithms checkpoint is playable.
