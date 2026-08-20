# Feature Specification: Discussion pinning and Q&A moderation

**Story**: CH-S25  
**Epic**: Teach & learn  
**Priority**: Should  
**Status**: Implemented  
**Created**: 2026-08-20

## User story

**As a** teacher,  
**I want** to pin important questions and hide inappropriate Q&A posts,  
**so that** students see the best answers first and the thread stays on-topic.

## Business value

Course Q&A exists but lacks curation; pinning and hiding give instructors lightweight moderation without a separate forum product.

## Scope

**In scope**
- Pin/unpin questions (pinned appear first on course detail and player Q&A tab)
- Hide/unhide questions and answers (hidden from students; visible to course owner with badge)
- Moderation controls on course editor and course detail for the owning teacher
- Seed: pinned FAQ on Algorithms

**Out of scope**
- Editing student text; reporting workflow; bulk moderation
- Live chat moderation (CH-S08)

## Preconditions

- CH-S17 auth; existing Q&A on course detail and player.
- Demo: `teacher@campushub.local` / `CampusHub!123`, Algorithms course.

## Screens

| Screen | URL | Actor | Observable result |
|---|---|---|---|
| Course detail Q&A | `/catalog/{id}#qa` | teacher / student | Pinned first; students skip hidden posts |
| Course editor | `/catalog/{id}/edit` | teacher | Pin, hide question, hide answer |
| Player Q&A tab | `/learn/{id}?tab=qa` | teacher / student | Same ordering and visibility as detail |

## Acceptance criteria

1. **Given** I own the course, **when** I pin a question, **then** it shows a Pinned badge and sorts above unpinned questions for everyone.
2. **Given** I hide a question or answer, **when** a student loads Q&A, **then** that post is omitted.
3. **Given** I hid a post, **when** I open Q&A as the course owner, **then** I still see it marked Hidden with an unhide action.
4. **Given** I am not the course owner, **when** I call pin/hide APIs, **then** access is denied.
5. **Given** I hide a pinned question, **when** save succeeds, **then** it is unpinned and hidden from students.

## Definition of Done

- [x] Acceptance criteria pass in UAT on the gateway
- [x] Automated test `[Trait("Story", "CH-S25")]`
- [x] Mock/seed data makes the screens clickable
- [x] Spec, plan, and code cite `CH-S25`

## Assumptions and dependencies

- Depends on: existing course Q&A (CH-S09 discovery detail, CH-S20 player)
- Assumptions: soft-hide only; no audit log

## Qualified story

| Field | Value |
|---|---|
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ **Done** |

### Apply (code)

- `QuestionModerationRules.cs`, pin/hide endpoints in `CourseLearningEndpoints.cs` (CH-S25)
- `course-detail.ts`, `course-player.ts`, `course-editor.ts`, `catalog.api.ts`
- Seed pinned Algorithms FAQ in `CatalogSeeder.cs`
