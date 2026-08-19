# Feature Specification: Teacher course analytics

**Story**: CH-S07  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - See course stats (Priority: P1)

A teacher opens `/catalog/:id/analytics` for enrollments, revenue, completions, rating, and lecture completion counts.

**Independent Test**: Sign in as teacher, My courses → Analytics on a published course.

**Acceptance Scenarios**:

1. **Given** a course owner, **When** they open analytics, **Then** confirmed enrollments and revenue show.
2. **Given** lecture completions, **When** stats load, **Then** per-lecture completion counts appear.

## Requirements

- **FR-001**: Stats MUST require catalog manage + owner (or admin).
- **FR-002**: Enrollment totals MAY come from Enrollment gateway.

## Success Criteria

- **SC-001**: Analytics is linked from My courses and the editor.
