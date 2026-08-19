# Feature Specification: [FEATURE NAME]

**Story**: CH-Snn  
**Jira idea**: MDP-nn  
**Workflow**: Specify → Apply → Test → Mock → Retest → Done  
**Feature Branch**: `[###-feature-name]`
**Created**: [DATE]
**Status**: Draft
**Input**: User description: "$ARGUMENTS"

## Qualified header *(mandatory)*

| Field | Value |
|---|---|
| Persona | [role] |
| Value | [why] |
| Screens | [gateway routes] |
| Code | [files from plan] |
| Tests | `[Trait("Story", "CH-Snn")]` + screen smoke |
| Mock | [seed / demo path] |

## Screens *(mandatory)*

| Screen | URL | Actor | Must show |
|---|---|---|---|
| [Name] | `http://localhost:5000/...` | student/teacher/admin | [visible result] |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value]

**Independent Test**: [How to verify this story alone]

**Acceptance Scenarios**:

1. **Given** [state], **When** [action], **Then** [outcome]

---

### Edge Cases

- What happens when [boundary]?

## Requirements *(mandatory)*

- **FR-001**: System MUST [capability]

## Success Criteria *(mandatory)*

- **SC-001**: [Observable outcome a human can check in the running app]

## Apply / Test / Mock / Done

- [ ] Specify finished
- [ ] Apply (code cites CH-Snn)
- [ ] Test (`dotnet test --filter Story=CH-Snn` and screen smoke)
- [ ] Mock in progress (seed)
- [ ] Retest on mock
- [ ] Done

## Assumptions

- Existing auth, gateway, and seeded users are reused unless this spec says otherwise.
