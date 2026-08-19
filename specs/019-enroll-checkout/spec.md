# Feature Specification: Enroll, checkout, and mock payment

**Story**: CH-S19  
**Workflow**: Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done**  
**Created**: 2026-08-20  
**Status**: Implemented

## Qualified header

| Field | Value |
|---|---|
| Persona | Student (JWT after CH-S17) |
| Screens | `/catalog/:id` Enroll, `/enroll/:courseId` checkout, `/enroll` my enrollments |
| Code | enrollment-mfe, Enrollment.Api saga, Payment.Api (internal) |

## A–Z

1. Login as `student@`.
2. Open a published course (`/catalog/{id}`).
3. Enroll → `/enroll/:courseId`.
4. **Pay successfully** or **Simulate payment failure** (mock PSP, no card network).
5. Saga: reserve seat (plan seat cap from JWT) → payment intent (internal key, not YARP) → confirm or compensate.
6. Confirmed enrollment: player unlocks; Access issues a CoursePass (CH-S21); Notification event (CH-S05).
7. `/enroll` lists mine. Staff may open the player without paying.

## Requirements

- **FR-001**: Browser MUST only talk to Enrollment via the gateway, never Payment `:5104`.
- **FR-002**: Failed pay MUST release the reserved seat.
- **FR-003**: Seeded catalog has **no** pre-made student enrollment — demo enrolls Algorithms or Linear Algebra in the UI.

## Success Criteria

- **SC-001**: Student can mock-pay into Algorithms and then open the player.
