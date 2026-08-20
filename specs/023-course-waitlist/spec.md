# Feature Specification: Course waitlist

**Story**: CH-S23  
**Epic**: Teach & learn  
**Priority**: Should  
**Status**: Implemented  
**Created**: 2026-08-20

## User story

**As a** student,  
**I want** to join a waitlist when a published course is full,  
**so that** I keep my place and can enrol when seats open again.

## Business value

Captures demand on sold-out courses instead of losing interested students at a dead-end “not open” message.

## Scope

**In scope**
- Join / leave waitlist when published and full
- Queue position on course detail and My enrollments
- Seed Distributed Systems as full for demos
- Remove waitlist entry after confirmed enrollment

**Out of scope**
- Auto-charge or auto-start checkout when a seat opens
- Push/email notifications for promotion (use enroll CTA when seats return)
- Teacher reordering of the queue
- Cross-tenant waitlists

## Preconditions

- Authenticated via CH-S17.
- Course is Published; `RemainingSeats == 0`.
- Seeded demo: `CampusHub!123` at `http://localhost:5000`.

## Screens

| Screen | URL | Actor | Observable result |
|---|---|---|---|
| Course detail (full) | `/catalog/{id}` | student | Join waitlist CTA; seat count; position after join |
| My enrollments | `/enroll` | student | Waitlist section with course + position |

## Acceptance criteria

1. **Given** a published full course and I am not enrolled, **when** I open course detail, **then** I see Join waitlist (not Enroll now) and remaining seats as 0.
2. **Given** I am not waitlisted, **when** I join, **then** I see my queue position and the course appears under Waitlist on `/enroll`.
3. **Given** I am waitlisted, **when** I leave the waitlist, **then** I am removed and can join again while the course stays full.
4. **Given** a course with open seats, **when** I try to join the waitlist, **then** the API rejects and the UI shows Enroll now instead.
5. **Given** I complete enrollment, **when** the saga confirms, **then** I am no longer on that course’s waitlist.

## Definition of Done

- [x] Acceptance criteria pass in UAT on the gateway
- [x] Automated test `[Trait("Story", "CH-S23")]`
- [x] Mock/seed data makes the screens clickable
- [x] Spec, plan, and code cite `CH-S23`

## Assumptions and dependencies

- Depends on: CH-S17, CH-S19 (enroll), Catalog capacity / reserve
- Assumptions: Enrollment owns waitlist rows; Catalog remains source of seat counts

## Qualified story

| Field | Value |
|---|---|
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ **Done** |

### Apply (code)

- `WaitlistEndpoints.cs`, `WaitlistRules.cs`, `CourseWaitlist` (CH-S23)
- `course-detail.ts`, `my-enrollments.ts`, `enrollment.api.ts`
- Catalog seed: Distributed `RemainingSeats = 0`
