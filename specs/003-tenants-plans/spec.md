# Feature Specification: Tenants, campus signup, and plan gates

**Story**: CH-S01  
**Status**: Implemented  
**Created**: 2026-08-19

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
