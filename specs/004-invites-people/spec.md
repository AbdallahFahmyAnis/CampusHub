# Feature Specification: Invites and People

**Story**: CH-S02  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Invite a member (Priority: P1)

A campus admin sends an invite. The invitee accepts and appears on People.

**Independent Test**: Open `/people`, create invite, accept `/invite/:token`.

**Acceptance Scenarios**:

1. **Given** an admin, **When** they invite an email, **Then** a tokenized invite exists.
2. **Given** a valid token, **When** the invitee accepts, **Then** they join the tenant People list.

## Requirements

- **FR-001**: Invites MUST be tenant-scoped.
- **FR-002**: People MUST list campus members.

## Success Criteria

- **SC-001**: Invite accept is visible without using a platform admin account.
