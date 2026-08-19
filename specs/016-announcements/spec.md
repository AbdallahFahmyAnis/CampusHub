# Feature Specification: Course announcements

**Story**: CH-S14  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Post and read announcements (Priority: P1)

Teachers post announcements. Students read them in the player Announcements tab.

**Independent Test**: Editor post; player tab; seeded Linear Algebra / Distributed posts.

**Acceptance Scenarios**:

1. **Given** a teacher, **When** they post, **Then** students see title body and author.
2. **Given** no posts, **When** students open the tab, **Then** an empty message shows.

## Requirements

- **FR-001**: POST announcements MUST require catalog manage + owner.
- **FR-002**: GET MUST be available to signed-in course viewers.

## Success Criteria

- **SC-001**: Seeded announcements appear after Catalog seed.

## Qualified story

| Field | Value |
|---|---|
| **Jira idea** | [MDP-25](https://abdallah-fahmy.atlassian.net/browse/MDP-25) |
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done** |

### Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| Editor announcements | `http://localhost:5000/catalog/{id}/edit` | teacher | Post title + body |
| Player Announcements | `http://localhost:5000/learn/course/{id}` | student | List with author/time |

### Apply (code)

- `AnnouncementEndpoints.cs` (CH-S14)
- `course-editor.ts`, `course-player.ts`

### Test / Mock

- Seeded Linear Algebra / Distributed posts. Empty tab message when none.
