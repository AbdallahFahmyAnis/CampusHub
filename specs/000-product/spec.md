# Feature Specification: CampusHub product

**Feature Branch**: `main`
**Created**: 2026-08-19
**Status**: Implemented (living)
**Input**: Brownfield educational SaaS already running locally

## Summary

CampusHub is a production-shaped campus learning platform: Angular MFEs behind a YARP BFF, .NET microservices, mock payments, chat, QR passes, and notifications. This spec is the product baseline. New work is a numbered spec under `specs/`, not an undocumented chat slice.

## User Scenarios & Testing

### User Story 1 - Sign in and browse (Priority: P1)

A student, teacher, or admin signs in through the gateway and uses the Angular shell.

**Independent Test**: Open `http://localhost:5000`, sign in with a seeded account, browse `/catalog`.

**Acceptance Scenarios**:

1. **Given** seeded users, **When** they sign in with `CampusHub!123`, **Then** they reach the catalog shell.
2. **Given** Meilisearch is down, **When** they search, **Then** SQL fallback still returns courses.

### User Story 2 - Teach and learn a course (Priority: P1)

Teachers publish courses with curriculum, quizzes, assignments, announcements, and a gradebook. Students enroll (mock pay), watch lectures, take quizzes, submit work, take notes, and see grades.

**Independent Test**: Teacher edits Algorithms or Linear Algebra; student opens the course player.

**Acceptance Scenarios**:

1. **Given** an enrolled student, **When** they open the player, **Then** they can use lecture, notes, Ask AI, quiz, assignments, announcements, grades, Q&A, and reviews.
2. **Given** a teacher, **When** they open My courses, **Then** they can edit, open gradebook, and open analytics.

### User Story 3 - Campus operations (Priority: P2)

Admins use `/ops`. Campus owners use `/campus`, people, invites, and mock billing. Notifications arrive in-app. Completing lectures can issue a certificate.

**Independent Test**: Sign in as admin and open Ops; as student open inbox and certificates.

## Shipped slices (baseline, real product order)

See [`specs/STORIES.md`](../STORIES.md) for A–Z walkthroughs. Auth is **CH-S17** (login, campus/invite/ops register, access token, refresh, logout).

1. Auth session (CH-S17)
2. Account profile (CH-S18)
3. Tenants / campus signup / plan gates (CH-S01)
4. Invites + People (CH-S02)
5. Catalog filters, sort, recommended (CH-S09)
6. Enroll + mock payment (CH-S19)
7. Course player / curriculum (CH-S20)
8. Lecture notes (CH-S13)
9. Chat + Ask AI (CH-S08)
10. Quizzes (CH-S11)
11. Assignments (CH-S12)
12. Due dates + calendar (CH-S16)
13. Announcements (CH-S14)
14. Gradebook (CH-S15)
15. Progress dashboard (CH-S10)
16. Notifications + SSE (CH-S05)
17. Certificates (CH-S06)
18. Course pass QR + attendance (CH-S21)
19. Teacher analytics (CH-S07)
20. Mock billing (CH-S03)
21. Platform `/ops` vs campus `/campus` (CH-S04)

Story index and Jira import: [`specs/jira/README.md`](../jira/README.md). Qualified cards: [`specs/STORIES.md`](../STORIES.md). Workflow: [`specs/WORKFLOW.md`](../WORKFLOW.md).

Each story is **Done** when Specify, Apply, Test, Mock, and Retest are complete. Code cites `CH-Snn` via `SddStories`.

## Requirements

- **FR-001**: System MUST keep the gateway as the only browser-facing edge.
- **FR-002**: System MUST keep seeded demo users working after each slice.
- **FR-003**: New slices MUST add a spec under `specs/NNN-slug/` before code.
- **FR-004**: Catalog learning features MUST remain usable if Meilisearch or an AI key is missing.

## Success Criteria

- **SC-001**: A new contributor can read `.specify/memory/constitution.md` and this spec and know where to add the next slice.
- **SC-002**: "Proceed to next" produces or consumes a spec, then implements that spec only.

## Next (backlog, unspecified)

Pick one, write `specs/NNN-slug/spec.md`, then implement:

- Course resources (syllabus links / extra materials)
- Waitlist when a course is full
- Teacher roster from enrollments (cross-service)
- Discussion pinning / Q&A moderation
