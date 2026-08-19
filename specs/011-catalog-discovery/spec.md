# Feature Specification: Catalog filters, sort, and recommended

**Story**: CH-S09  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Find a course (Priority: P1)

A student filters by category, level, price, rating, sorts, and sees recommended courses.

**Independent Test**: `/catalog` with filters; recommended strip; search with Meilisearch down.

**Acceptance Scenarios**:

1. **Given** filters, **When** the list loads, **Then** items match filters and totalCount is consistent.
2. **Given** Meilisearch down, **When** they search, **Then** SQL fallback returns courses.

## Requirements

- **FR-001**: Rating filter MUST apply before paging.
- **FR-002**: Recommended MUST return a list for signed-in users.

## Success Criteria

- **SC-001**: Empty filter results do not show a misleading non-zero total.
