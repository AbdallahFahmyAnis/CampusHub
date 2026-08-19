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

## Qualified story

| Field | Value |
|---|---|
| **Jira idea** | [MDP-15](https://abdallah-fahmy.atlassian.net/browse/MDP-15) |
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done** |

### Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| Platform ops | `http://localhost:5000/ops` | `admin@campushub.local` | Razor platform console |
| Campus home | `http://localhost:5000/campus` | campus admin | Tenant dashboard, link to ops only if platform admin |

### Apply (code)

- Gateway `/ops` Razor
- `src/frontend/projects/shell/src/app/campus-dashboard.ts` (CH-S04)

### Test / Mock

- Admin: both URLs. Campus owner: `/campus` only, not platform-wide tools.
