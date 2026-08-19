# Feature Specification: Notifications and SSE

**Story**: CH-S05  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Inbox and live bell (Priority: P1)

A student receives in-app notifications (enrollment, completion) and sees live unread updates.

**Independent Test**: Trigger an enrollment confirmation, open `/learn/inbox`, watch the shell bell.

**Acceptance Scenarios**:

1. **Given** EnrollmentConfirmed, **When** Notification processes it, **Then** an inbox row exists.
2. **Given** an open shell, **When** a new notification arrives, **Then** unread count can update via SSE.

## Requirements

- **FR-001**: Notification service MUST own inbox storage.
- **FR-002**: Shell MUST read unread count through the gateway.

## Success Criteria

- **SC-001**: Inbox is usable without email being configured.
