# Feature Specification: Platform Ops vs campus console

**Story**: CH-S04  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Split consoles (Priority: P1)

Platform administrators use Razor `/ops`. Campus owners use `/campus` in the Angular shell.

**Independent Test**: Sign in as `admin@campushub.local` → `/ops`. Sign in as a campus owner → `/campus`.

**Acceptance Scenarios**:

1. **Given** a platform admin, **When** they open `/ops`, **Then** they see health and ops tools.
2. **Given** a campus user, **When** they open `/campus`, **Then** they do not get platform-wide ops.

## Requirements

- **FR-001**: `/ops` MUST be platform-scoped.
- **FR-002**: `/campus` MUST be tenant-scoped.

## Success Criteria

- **SC-001**: The two consoles are distinct URLs and audiences.
