# Feature Specification: Student progress dashboard

**Story**: CH-S10  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - My learning (Priority: P1)

A student sees streak, lecture counts, course progress bars, and continue-learning.

**Independent Test**: Sign in as student, open `/learn`.

**Acceptance Scenarios**:

1. **Given** lecture progress, **When** they open My learning, **Then** courses and percents show.
2. **Given** an in-progress course, **When** they click Continue, **Then** the player opens the next lecture.

## Requirements

- **FR-001**: Dashboard MUST use Catalog progress data.
- **FR-002**: Continue link MUST deep-link to a lecture when possible.

## Success Criteria

- **SC-001**: My learning is the default `/learn` route.
