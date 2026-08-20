# Feature Specification: Course resources

**Story**: CH-S22  
**Epic**: Teach & learn  
**Priority**: Should  
**Status**: Implemented  
**Created**: 2026-08-20

## User story

**As a** teacher,  
**I want** to publish syllabus links and extra reading materials on a course,  
**so that** students find supporting resources without leaving the player.

## Business value

Keeps official course links in one place so students spend less time hunting for syllabus and readings outside CampusHub.

## Scope

**In scope**
- Teacher adds a resource with title, https URL, and optional description
- Students (and staff) list resources on a player Resources tab
- Seeded demo resources on Algorithms / Linear Algebra

**Out of scope**
- File uploads / blob storage
- Per-lecture resources
- Link scraping or preview cards
- Ordering drag-and-drop beyond newest-first list

## Preconditions

- Caller is authenticated via CH-S17.
- Course exists; teacher owns or can manage catalog (same rule as announcements).
- Seeded demo: `CampusHub!123` at `http://localhost:5000`.

## Screens

| Screen | URL | Actor | Observable result |
|---|---|---|---|
| Editor resources | `http://localhost:5000/catalog/{id}/edit` | teacher | Resources panel: list + add title/URL/description |
| Player Resources | `http://localhost:5000/learn/course/{id}` | student | Resources tab lists title, link, description; empty state when none |

## Acceptance criteria

1. **Given** I can manage the course, **when** I add a resource with title and `https` URL, **then** it appears in the editor list and on the player Resources tab.
2. **Given** no resources, **when** a student opens Resources, **then** an empty state explains that the instructor has not added materials yet.
3. **Given** a missing or non-http(s) URL, **when** I submit, **then** the API rejects the create and the UI shows an error.
4. **Given** I am not allowed to manage the course, **when** I POST a resource, **then** access is denied.

## Definition of Done

- [x] Acceptance criteria pass in UAT on the gateway
- [x] Automated test `[Trait("Story", "CH-S22")]`
- [x] Mock/seed data makes the screens clickable
- [x] Spec, plan, and code cite `CH-S22`

## Assumptions and dependencies

- Depends on: CH-S17 (auth), CH-S20 (course player)
- Assumptions: Catalog owns course resources; links are external (no file store in this slice)

## Qualified story

| Field | Value |
|---|---|
| **Workflow** | Specify ✅ Apply ✅ Test ✅ Mock ✅ **Done** |

### Apply (code)

- `ResourceEndpoints.cs`, `CourseResourceRules.cs` (CH-S22)
- `course-editor.ts`, `course-player.ts`, `catalog.api.ts`
