# Feature Specification: Invites and People

**Story**: CH-S02  
**Status**: Implemented  
**Created**: 2026-08-19

## A–Z

1. Login as campus admin (CH-S17).
2. `/people` create invite.
3. Invitee `/invite/:token` sets password (register).
4. Login as the new member.

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

## Qualified story

| Field | Value |
|---|---|
| **Jira idea** | [MDP-13](https://abdallah-fahmy.atlassian.net/browse/MDP-13) |
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done** |

### Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| People | `http://localhost:5000/people` | campus admin | Member list + invite form |
| Accept invite | `http://localhost:5000/invite/:token` | invitee | Join campus |

### Apply (code)

- Identity `CampusEndpoints` invites/members (CH-S02)
- `src/frontend/projects/shell/src/app/people.ts`

### Test / Mock

- Create invite as admin; open token URL; member appears on People. Seeded campus already has members.
