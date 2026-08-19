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

## Qualified story

| Field | Value |
|---|---|
| **Jira idea** | [MDP-22](https://abdallah-fahmy.atlassian.net/browse/MDP-22) |
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done** |
| **Persona** | Teacher authors; student takes |

### Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| Editor quizzes | `http://localhost:5000/catalog/{id}/edit` | teacher | Add quiz (title, pass %, questions) |
| Player Quiz tab | `http://localhost:5000/learn/course/{id}` | student | Start, submit, percent, pass/fail |

Algorithms id: `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1`.

### Apply (code)

- `QuizEndpoints.cs`, `QuizScoring.cs` (CH-S11 / MDP-22)
- `course-editor.ts`, `course-player.ts`
- Schema `CourseQuizzes` / `CourseQuizAttempts` (no brittle FK)

### Test

- Automated: `dotnet test --filter Story=CH-S11` (`QuizScoringTests`)
- Screen: teacher adds quiz; student submits; list must not 500 if attempts table was missing (ensure creates it)

### Mock

- Seeded Algorithms checkpoint quiz, pass 70%.
