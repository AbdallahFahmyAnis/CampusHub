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
