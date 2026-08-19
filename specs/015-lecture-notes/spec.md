# Feature Specification: Lecture notes

**Story**: CH-S13  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Persist lecture notes (Priority: P1)

A student writes notes on a lecture. They persist on the account and appear on My learning.

**Independent Test**: Player Notes tab, refresh, confirm text; `/learn` notes list.

**Acceptance Scenarios**:

1. **Given** an unlocked lecture, **When** they type notes, **Then** a later visit shows the same text.
2. **Given** saved notes, **When** they open My learning, **Then** snippets link back to the lecture.

## Requirements

- **FR-001**: Notes MUST be unique per course+lecture+student.
- **FR-002**: Empty notes SHOULD not clutter My learning.

## Success Criteria

- **SC-001**: Notes survive Catalog restart (SQLite).
