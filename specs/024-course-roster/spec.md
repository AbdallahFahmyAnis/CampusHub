# Feature Specification: Teacher enrollment roster

**Story**: CH-S24  
**Epic**: Teach & learn  
**Priority**: Should  
**Status**: Implemented  
**Created**: 2026-08-20

## User story

**As a** teacher,  
**I want** a roster of confirmed enrollments for my course,  
**so that** I see every enrolled student even before they submit quizzes or assignments.

## Business value

Gradebook only lists students with submissions; the roster closes that gap with authoritative enrollment data from the Enrollment service.

## Scope

**In scope**
- Teacher roster at `/catalog/{id}/roster` with name, email, enrolled date
- Catalog authorizes; Enrollment is source of truth (internal API)
- Confirmed enrollments only
- Seed demo rows for Algorithms and Linear Algebra

**Out of scope**
- Editing or removing enrollments from roster
- In-progress / failed checkout rows
- Export CSV

## Preconditions

- CH-S17 auth, CH-S19 enroll saga, teacher owns course or can manage catalog.
- Demo: `teacher@campushub.local` / `CampusHub!123`.

## Screens

| Screen | URL | Actor | Observable result |
|---|---|---|---|
| Course roster | `/catalog/{id}/roster` | teacher | Table of confirmed students with enrolled date |
| Editor link | `/catalog/{id}/edit` | teacher | Roster button beside Gradebook |

## Acceptance criteria

1. **Given** I own the course, **when** I open Roster, **then** I see confirmed enrollments with student name, email, and enrolled date ordered oldest first.
2. **Given** a student is confirmed but has no quiz/assignment submissions, **when** I open Roster, **then** they still appear (unlike gradebook).
3. **Given** I am not the course owner and not admin, **when** I request the roster API, **then** access is denied.
4. **Given** no confirmed enrollments, **when** I open Roster, **then** an empty state is shown.

## Definition of Done

- [x] Acceptance criteria pass in UAT on the gateway
- [x] Automated test `[Trait("Story", "CH-S24")]`
- [x] Mock/seed data makes the screen clickable
- [x] Spec, plan, and code cite `CH-S24`

## Assumptions and dependencies

- Depends on: CH-S15 (gradebook distinction), CH-S19 (enrollment confirm)
- Catalog calls Enrollment via existing internal gateway pattern (same as analytics stats)

## Qualified story

| Field | Value |
|---|---|
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ **Done** |

### Apply (code)

- `RosterEndpoints.cs`, `EnrollmentGateway.GetRosterAsync`, `EnrollmentEndpoints.InternalRoster` (CH-S24)
- `course-roster.ts`, nav links from editor / gradebook / analytics / My courses
