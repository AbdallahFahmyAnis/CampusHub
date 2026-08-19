# Feature Specification: Tenants, campus signup, and plan gates

**Story**: CH-S01  
**Status**: Implemented  
**Created**: 2026-08-19

## A–Z

1. Anonymous `/signup` **or** login (CH-S17).
2. Tenant + plan on session (`/whoami`).
3. Plan gates Ask AI / chat / seats.

## User Story 1 - Sign up a campus (Priority: P1)

A campus owner creates a tenant and works under a plan (Free vs Campus) that gates features such as model Ask AI and seat counts.

**Independent Test**: Open `/signup`, complete campus signup, sign in, confirm plan on account/billing.

**Acceptance Scenarios**:

1. **Given** a new campus, **When** signup succeeds, **Then** a tenant exists and the owner can sign in.
2. **Given** Free plan, **When** Ask AI runs, **Then** answers come from course materials only.

## Requirements

- **FR-001**: System MUST attach users to a tenant.
- **FR-002**: System MUST enforce plan gates in product features.

## Success Criteria

- **SC-001**: Seeded `admin@` / `teacher@` / `student@` still work with `CampusHub!123`.

## Qualified story

| Field | Value |
|---|---|
| **Jira idea** | [MDP-12](https://abdallah-fahmy.atlassian.net/browse/MDP-12) |
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done** |
| **Persona** | Campus owner |
| **Value** | Isolate campuses and gate paid features by plan |

### Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| Campus signup | `http://localhost:5000/signup` | anonymous | Create campus + owner account |
| Catalog after login | `http://localhost:5000/catalog` | owner | Tenant-scoped courses |

### Apply (code)

- `src/services/identity/CampusHub.Identity.Api/Features/CampusEndpoints.cs` (CH-S01)
- `src/frontend/projects/shell/src/app/signup.ts`
- Catalog Ask AI plan gate in `CourseLearningEndpoints.cs` / `CourseTutor.cs`

### Test

- Screen: complete signup, sign in, confirm tenant.
- Free plan Ask AI stays catalog-text.

### Mock

- Seeded `admin@` / `teacher@` / `student@` + `CampusHub!123` on the default campus.
