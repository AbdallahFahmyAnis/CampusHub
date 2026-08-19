# Feature Specification: Course pass QR and attendance

**Story**: CH-S21  
**Workflow**: Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done**  
**Created**: 2026-08-20  
**Status**: Implemented

## User story

**As an** enrolled student, **I want** a QR course pass.  
**As a** teacher, **I want** to scan it,  
**so that** attendance can be recorded.

## Qualified header

| Field | Value |
|---|---|
| Persona | Student (pass), teacher (scan) |
| Screens | `/learn/pass`, `/learn/attendance` |
| Code | `AccessEndpoints.cs`, `course-pass.ts`, `attendance.ts` |

## A–Z

1. Login (CH-S17). Confirm enrollment (CH-S19).
2. Access issues a **CoursePass** credential (not seeded until enroll).
3. Student opens `/learn/pass` — QR PNG from `GET /api/access/credentials/{id}/qr`.
4. Teacher opens `/learn/attendance`, pastes/scans token (`POST /api/access/scans`).
5. **Revoke credential** (not OAuth): `EnrollmentCancelled` marks the pass Revoked. Scan of unknown/revoked/expired fails.
6. Certificates (completion) are CH-S06 on `/learn/certificates`.

## Success Criteria

- **SC-001**: After mock enroll, student sees a pass; teacher scan of a bad token fails clearly.
