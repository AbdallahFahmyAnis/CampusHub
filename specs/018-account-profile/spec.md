# Feature Specification: Account profile and password

**Story**: CH-S18  
**Workflow**: Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done**  
**Created**: 2026-08-20  
**Status**: Implemented

## User story

**As a** signed-in user,  
**I want** to update my display name and password,  
**so that** my identity stays accurate without changing email.

## Qualified header

| Field | Value |
|---|---|
| Persona | Any signed-in user (after CH-S17) |
| Screens | `http://localhost:5000/account` |
| Code | `profile.ts`, Gateway `AccountEndpoints.cs`, Identity `UserEndpoints.cs` |

## A–Z

1. Login (CH-S17).
2. Open `/account`.
3. Change display name (`PUT /api/account/me`). Email is read-only.
4. Change password (`POST /api/account/password`) with Identity complexity.
5. Name updates the shell session signal; JWT `name` updates on **next login**.
6. Password change does **not** revoke other sessions (logout still CH-S17).

## Requirements

- **FR-001**: Profile MUST require an authenticated BFF session.
- **FR-002**: Email MUST not be editable on this screen.

## Success Criteria

- **SC-001**: Signed-in student can open Account and see their email.
