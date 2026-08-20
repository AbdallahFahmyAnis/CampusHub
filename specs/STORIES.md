# CampusHub user stories (BA backlog)

Professional backlog for CampusHub. Each item is an independent user story: **As a / I want / So that**, with scope, acceptance criteria, and definition of Done.

**Product order** (how a user actually uses the platform). Gateway: `http://localhost:5000`.  
**Workflow:** Specify → Apply → Test → Mock → Done ([WORKFLOW.md](WORKFLOW.md)).  
**Demo accounts:** `student@`, `teacher@`, `admin@campushub.local` / `CampusHub!123`.

| ID | Priority | Epic | User story title | Spec |
|---|---|---|---|---|
| CH-S17 | Must | Platform | Authenticate and manage a secure session | [017](017-auth-session/spec.md) |
| CH-S18 | Must | Platform | Maintain my profile and password | [018](018-account-profile/spec.md) |
| CH-S01 | Must | Platform | Register a campus and apply plan entitlements | [003](003-tenants-plans/spec.md) |
| CH-S02 | Must | Platform | Invite people to my campus | [004](004-invites-people/spec.md) |
| CH-S09 | Must | Discovery | Discover and filter courses | [011](011-catalog-discovery/spec.md) |
| CH-S19 | Must | Teach & learn | Enrol and complete mock checkout | [019](019-enroll-checkout/spec.md) |
| CH-S20 | Must | Teach & learn | Consume a course curriculum | [020](020-course-player/spec.md) |
| CH-S13 | Should | Teach & learn | Capture lecture notes | [015](015-lecture-notes/spec.md) |
| CH-S08 | Should | Discovery | Discuss the course and ask the tutor | [010](010-chat-ai-tutor/spec.md) |
| CH-S11 | Must | Teach & learn | Author and sit a quiz | [013](013-quizzes/spec.md) |
| CH-S12 | Must | Teach & learn | Set and submit written assignments | [014](014-assignments/spec.md) |
| CH-S16 | Should | Teach & learn | Schedule assignment due dates | [002](002-assignment-due-dates/spec.md) |
| CH-S14 | Should | Teach & learn | Publish course announcements | [016](016-announcements/spec.md) |
| CH-S15 | Must | Teach & learn | Review course grades | [001](001-course-gradebook/spec.md) |
| CH-S10 | Should | Discovery | Track my learning progress | [012](012-progress-dashboard/spec.md) |
| CH-S05 | Should | Discovery | Receive in-app notifications | [007](007-notifications/spec.md) |
| CH-S06 | Should | Discovery | Receive a completion certificate | [008](008-certificates/spec.md) |
| CH-S21 | Should | Discovery | Present a course pass for attendance | [021](021-course-pass/spec.md) |
| CH-S07 | Should | Teach & learn | Inspect course analytics | [009](009-course-analytics/spec.md) |
| CH-S03 | Should | Platform | Manage campus plan (mock billing) | [005](005-mock-billing/spec.md) |
| CH-S04 | Must | Platform | Separate platform ops from campus admin | [006](006-ops-campus/spec.md) |
| CH-S22 | Should | Teach & learn | Publish course resource links | [022](022-course-resources/spec.md) |
| CH-S23 | Should | Teach & learn | Join a waitlist when a course is full | [023](023-course-waitlist/spec.md) |
| CH-S24 | Should | Teach & learn | View confirmed enrollment roster | [024](024-course-roster/spec.md) |
| CH-S25 | Should | Teach & learn | Pin and moderate course Q&A | [025](025-discussion-moderation/spec.md) |

Status for CH-S01–S25: **Done** (implemented). CH-S01–S16 = Jira Discovery MDP-12–MDP-27. CH-S17–S25 = in-repo until the next Jira import.

---

## CH-S17 — Authenticate and manage a secure session

**As a** campus user,  
**I want** to register through an approved path, sign in, stay signed in safely, and sign out,  
**so that** only I can use my account and the application never exposes tokens in the browser.

| | |
|---|---|
| **Priority** | Must |
| **Epic** | Platform |
| **Value** | Protects student and staff data; is a dependency for every other story. |

**In scope:** campus registration, invite/ops user creation, login, access token on the BFF cookie, silent refresh, logout.  
**Out of scope:** public self-service student registration; OAuth token-revoke endpoint; social login; email verification.

**Preconditions:** Identity and gateway are running. Seeded users exist for UAT.

**Acceptance criteria**

1. **Given** a seeded student, **when** they sign in with a valid password, **then** they reach the catalog and `/whoami` reports authenticated identity, role, tenant, and plan.
2. **Given** a new campus owner, **when** they complete `/signup`, **then** they are directed to login and can sign in as Administrator of that campus.
3. **Given** a valid invite, **when** the invitee sets a password that meets policy, **then** they can sign in and appear on People.
4. **Given** an authenticated session, **when** the UI calls a catalog API, **then** the gateway attaches a Bearer access token and the browser does not store a JWT.
5. **Given** an access token due to expire within two minutes, **when** the session is validated, **then** the gateway refreshes the token without asking the user to sign in again.
6. **Given** the user chooses Sign out, **when** logout completes, **then** the session is ended and protected routes require login again.
7. **Given** an unauthenticated API call, **when** it hits `/api/*`, **then** the response is 401 (not an HTML login bounce).

**Definition of Done:** AC 1–7 in UAT; `dotnet test --filter Story=CH-S17`; no JWT in browser storage.

---

## CH-S18 — Maintain my profile and password

**As a** signed-in user,  
**I want** to update my display name and password,  
**so that** my identity in the product stays accurate without exposing my email to change.

**In scope:** `/account` name and password. **Out of scope:** email change; forcing other sessions to expire.

**Acceptance criteria**

1. **Given** I am signed in, **when** I open Account, **then** I see my email (read-only) and can save a new display name.
2. **Given** a valid current password, **when** I set a new password that meets policy, **then** I remain signed in and can use the new password at next login.

---

## CH-S01 — Register a campus and apply plan entitlements

**As a** campus owner,  
**I want** my organisation to exist as a tenant on a plan,  
**so that** seats, AI, and chat follow what we pay for.

**Acceptance criteria**

1. **Given** a successful campus signup, **when** the owner signs in, **then** session shows that tenant and plan.
2. **Given** Free plan, **when** a learner uses Ask AI, **then** answers come from course materials only (no model requirement).

---

## CH-S02 — Invite people to my campus

**As a** campus administrator,  
**I want** to invite staff and students by email/token,  
**so that** only authorised people join my campus.

**Acceptance criteria**

1. **Given** I am a campus admin, **when** I create an invite on People, **then** I receive a token URL I can copy (email delivery is not required).
2. **Given** a valid token, **when** the invitee accepts and sets a password, **then** they appear on the People list for that tenant.

---

## CH-S09 — Discover and filter courses

**As a** learner,  
**I want** to search, filter, sort, and see recommended courses,  
**so that** I can find a relevant offering without leaving the catalog.

**Acceptance criteria**

1. **Given** I am signed in, **when** I apply filters, **then** the list and total count match those filters.
2. **Given** search infrastructure is unavailable, **when** I search, **then** SQL fallback still returns courses.

---

## CH-S19 — Enrol and complete mock checkout

**As a** student,  
**I want** to enrol in a published course using a simulated payment,  
**so that** I can start learning without a live card network.

**Acceptance criteria**

1. **Given** a published course, **when** I pay successfully, **then** enrolment is confirmed and the player unlocks.
2. **Given** I simulate payment failure, **when** the saga completes, **then** the reserved seat is released and I am not enrolled.

---

## CH-S20 — Consume a course curriculum

**As an** enrolled student,  
**I want** to open lectures in order and mark them complete,  
**so that** I can progress through the syllabus.

**Acceptance criteria**

1. **Given** confirmed enrolment (or staff), **when** I open the player, **then** I can view the current lecture and complete it.
2. **Given** I am not enrolled, **when** I request a full lecture body or completion, **then** access is denied (preview lectures remain available).

---

## CH-S13 — Capture lecture notes

**As a** student,  
**I want** notes on a lecture to persist on my account,  
**so that** I can resume study later.

**Acceptance criteria**

1. **Given** an unlocked lecture, **when** I save notes and refresh, **then** the same text is shown.
2. **Given** saved notes, **when** I open My learning, **then** snippets link back to the lecture.

---

## CH-S08 — Discuss the course and ask the tutor

**As a** learner on a campus plan that allows chat,  
**I want** a course room and an Ask AI tutor,  
**so that** I can get help from peers and from course materials.

**Acceptance criteria**

1. **Given** enrolment (or staff), **when** I open course chat, **then** I can send messages.
2. **Given** no AI key or Free plan, **when** I ask a question, **then** I still receive an answer from catalog text.

---

## CH-S11 — Author and sit a quiz

**As a** teacher, **I want** to publish a multiple-choice quiz.  
**As a** student, **I want** to sit it and see a score,  
**so that** mastery is measured in the course.

**Acceptance criteria**

1. **Given** I am the course teacher, **when** I add a quiz, **then** it appears on the student Quiz tab.
2. **Given** I am enrolled, **when** I submit answers, **then** I see percent and pass/fail against the pass mark.
3. **Given** a missing attempts table at first run, **when** the quiz list loads, **then** the API does not return 500.

---

## CH-S12 — Set and submit written assignments

**As a** teacher, **I want** to set written work and grade it.  
**As a** student, **I want** to submit it,  
**so that** coursework is collected in one place.

**Acceptance criteria**

1. **Given** I am enrolled, **when** I submit text, **then** the teacher can see that submission.
2. **Given** I am the teacher, **when** I enter a score, **then** it is clamped to the maximum and the student can see score and feedback.

---

## CH-S16 — Schedule assignment due dates

**As a** teacher, **I want** an optional due date.  
**As a** student, **I want** upcoming and overdue work on My learning,  
**so that** I do not miss deadlines.

**Acceptance criteria**

1. **Given** a due date, **when** the student opens Assignments, **then** due, overdue, and late states are visible.
2. **Given** dated work in a course I am learning, **when** I open My learning, **then** items appear in calendar order.
3. **Given** no due date, **when** I view the calendar, **then** that assignment is omitted but remains submittable.

---

## CH-S14 — Publish course announcements

**As a** teacher, **I want** to post an announcement,  
**so that** the cohort sees timely course news.

**Acceptance criteria**

1. **Given** I can manage the course, **when** I post title and body, **then** students see it on the Announcements tab.
2. **Given** no posts, **when** a student opens the tab, **then** an empty state is shown.

---

## CH-S15 — Review course grades

**As a** teacher, **I want** a roster of quiz and assignment scores.  
**As a** student, **I want** only my own row,  
**so that** assessment is visible without leaking peers’ marks.

**Acceptance criteria**

1. **Given** I am the teacher, **when** I open the gradebook, **then** I see students who have submitted work and an overall percent.
2. **Given** I am a student, **when** I open Grades, **then** I do not see other students’ scores.

---

## CH-S10 — Track my learning progress

**As a** student,  
**I want** a dashboard of progress and a continue action,  
**so that** I can resume the right lecture quickly.

**Acceptance criteria**

1. **Given** lecture progress, **when** I open My learning, **then** I see courses and completion percents.
2. **Given** an in-progress course, **when** I choose Continue, **then** the player opens at a sensible lecture.

---

## CH-S05 — Receive in-app notifications

**As a** signed-in user,  
**I want** an inbox and a live unread count,  
**so that** I notice enrolment and teaching events without email.

**Acceptance criteria**

1. **Given** a relevant domain event, **when** Notification processes it, **then** an inbox row exists for me.
2. **Given** the shell is open, **when** a new notification arrives, **then** unread count can update without a full page reload.

---

## CH-S06 — Receive a completion certificate

**As a** student who finished every lecture,  
**I want** a certificate on my account,  
**so that** I can evidence completion.

**Acceptance criteria**

1. **Given** all lectures complete, **when** completion is processed, **then** a certificate is listed on Certificates.
2. **Given** no completions, **when** I open Certificates, **then** the empty state explains how to earn one.

---

## CH-S21 — Present a course pass for attendance

**As a** enrolled student, **I want** a QR course pass.  
**As a** teacher, **I want** to scan it,  
**so that** attendance can be recorded on campus.

**Acceptance criteria**

1. **Given** confirmed enrolment, **when** I open Course pass, **then** I can display a QR for my pass.
2. **Given** a revoked, expired, or unknown token, **when** a teacher scans, **then** the scan is rejected with a clear error.

---

## CH-S07 — Inspect course analytics

**As a** teacher,  
**I want** enrolment, revenue, and lecture-completion stats for my course,  
**so that** I can see whether the offering is working.

**Acceptance criteria**

1. **Given** I own the course (or I am admin), **when** I open analytics, **then** I see confirmed enrolments and per-lecture completion counts.
2. **Given** I am not allowed to manage the course, **when** I request stats, **then** access is denied.

---

## CH-S03 — Manage campus plan (mock billing)

**As a** campus administrator,  
**I want** to view and change plan on a mock billing page,  
**so that** we can demo upgrades without a live payment provider.

**Acceptance criteria**

1. **Given** I am campus admin, **when** I open Billing, **then** I see the current plan and mock invoices.
2. **Given** a successful mock upgrade, **when** I sign in again, **then** session plan matches the new entitlement.

---

## CH-S04 — Separate platform operations from campus administration

**As a** platform administrator, **I want** a platform console.  
**As a** campus owner, **I want** a campus console,  
**so that** tenant data is not mixed with platform-wide tools.

**Acceptance criteria**

1. **Given** a platform admin on the default campus, **when** they open Ops, **then** they see platform health and ops tools.
2. **Given** a campus-only admin, **when** they open Campus, **then** they do not receive platform-wide ops.

---

## CH-S22 — Publish course resource links

**As a** teacher,  
**I want** to publish syllabus links and extra reading materials on a course,  
**so that** students find supporting resources without leaving the player.

| | |
|---|---|
| **Priority** | Should |
| **Epic** | Teach & learn |
| **Value** | Official course links stay in one place; less time hunting outside CampusHub. |

**In scope:** title + https URL + optional description; editor panel; player Resources tab; seed on Algorithms / Linear Algebra.  
**Out of scope:** file uploads; per-lecture resources; link previews.

**Acceptance criteria**

1. **Given** I can manage the course, **when** I add a resource with title and https URL, **then** it appears in the editor and on the player Resources tab.
2. **Given** no resources, **when** a student opens Resources, **then** an empty state is shown.
3. **Given** a missing or non-http(s) URL, **when** I submit, **then** create is rejected.
4. **Given** I cannot manage the course, **when** I POST a resource, **then** access is denied.

---

## CH-S23 — Join a waitlist when a course is full

**As a** student,  
**I want** to join a waitlist when a published course is full,  
**so that** I keep my place and can enrol when seats open again.

| | |
|---|---|
| **Priority** | Should |
| **Epic** | Teach & learn |
| **Value** | Captures demand on sold-out courses instead of a dead-end message. |

**In scope:** join/leave, queue position, My enrollments waitlist section, full Distributed seed, clear on confirm.  
**Out of scope:** auto-checkout on seat open; promotion notifications; teacher reordering.

**Acceptance criteria**

1. **Given** a published full course, **when** I open detail, **then** I see Join waitlist and 0 seats.
2. **Given** I join, **when** the request succeeds, **then** I see my position and the course on `/enroll` Waitlist.
3. **Given** I am waitlisted, **when** I leave, **then** I am removed from the queue.
4. **Given** open seats, **when** I try to join waitlist, **then** the API rejects and UI shows Enroll now.
5. **Given** confirmed enrollment, **when** the saga completes, **then** I am not on that waitlist.

---

## CH-S24 — View confirmed enrollment roster

**As a** teacher,  
**I want** a roster of confirmed enrollments for my course,  
**so that** I see every enrolled student even before they submit quizzes or assignments.

| | |
|---|---|
| **Priority** | Should |
| **Epic** | Teach & learn |
| **Value** | Authoritative enrollment list; gradebook only shows students with submissions. |

**In scope:** `/catalog/{id}/roster`, Catalog auth + Enrollment internal API, seed Sam/Noah on Algorithms.  
**Out of scope:** cancel enrollment from roster; in-progress checkout rows.

**Acceptance criteria**

1. **Given** I own the course, **when** I open Roster, **then** I see confirmed students with name, email, enrolled date (oldest first).
2. **Given** a confirmed student with no submissions, **when** I open Roster, **then** they appear (unlike gradebook).
3. **Given** I cannot manage the course, **when** I call the roster API, **then** access is denied.
4. **Given** no confirmed enrollments, **when** I open Roster, **then** empty state is shown.

---

## CH-S25 — Pin and moderate course Q&A

**As a** teacher,  
**I want** to pin important questions and hide inappropriate Q&A posts,  
**so that** students see the best answers first and the thread stays on-topic.

| | |
|---|---|
| **Priority** | Should |
| **Epic** | Teach & learn |
| **Value** | Lightweight curation on existing course Q&A without a separate forum. |

**In scope:** pin/unpin, hide/unhide questions and answers, editor + detail + player UI, pinned Algorithms seed.  
**Out of scope:** edit student text; live chat moderation.

**Acceptance criteria**

1. **Given** I own the course, **when** I pin a question, **then** it shows Pinned and sorts first for everyone.
2. **Given** I hide a post, **when** a student loads Q&A, **then** that post is omitted.
3. **Given** I hid a post, **when** I open Q&A as owner, **then** I see it marked Hidden with unhide.
4. **Given** I am not the owner, **when** I call pin/hide APIs, **then** access is denied.
5. **Given** I hide a pinned question, **when** save succeeds, **then** it is unpinned and hidden from students.

---

## Definition of Done (all stories)

A story is Done when UAT on `http://localhost:5000` matches the acceptance criteria, mock data is clickable, tests tagged `Story=CH-Snn` pass where they exist, and the spec/plan/code cite the story id.
