# Feature Specification: Course completion certificates

**Story**: CH-S06  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Earn a certificate (Priority: P1)

When a student completes every lecture, Access issues a completion certificate listed on `/learn/certificates`.

**Independent Test**: Complete all lectures in an enrolled course, open Certificates.

**Acceptance Scenarios**:

1. **Given** all lectures complete, **When** CourseCompleted is processed, **Then** a certificate exists.
2. **Given** no completions, **When** they open Certificates, **Then** the empty state explains how to earn one.

## Requirements

- **FR-001**: Access MUST own certificates.
- **FR-002**: Catalog MUST emit completion when the last lecture is done.

## Success Criteria

- **SC-001**: Certificates page is reachable from My learning.
