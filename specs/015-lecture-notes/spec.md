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

## Qualified story

| Field | Value |
|---|---|
| **Jira idea** | [MDP-24](https://abdallah-fahmy.atlassian.net/browse/MDP-24) |
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done** |

### Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| Player Notes | `http://localhost:5000/learn/course/{id}` | student | Save notes on a lecture |
| My learning notes | `http://localhost:5000/learn` | student | Snippets linking to lectures |

### Apply (code)

- `NoteEndpoints.cs` (CH-S13)
- Player + `progress-dashboard.ts`

### Test / Mock

- Type notes, refresh, same text. Empty notes stay off the dashboard list.
