# Feature Specification: Mock billing

**Story**: CH-S03  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Review mock billing (Priority: P1)

A campus admin opens billing, sees plan and mock invoices, and can upgrade without a real card network.

**Independent Test**: Sign in as campus admin, open `/billing`.

**Acceptance Scenarios**:

1. **Given** a tenant, **When** they open billing, **Then** current plan is shown.
2. **Given** mock upgrade, **When** it succeeds, **Then** plan changes without a live PSP.

## Requirements

- **FR-001**: Billing MUST be mock for local/demo.
- **FR-002**: Plan changes MUST persist on the tenant.

## Success Criteria

- **SC-001**: Billing is usable at `http://localhost:5000/billing` after sign-in.
