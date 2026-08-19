# CampusHub SDD story map

Source of truth for Jira. Each story has a spec and a plan. Status is **Done** for shipped slices.

## Plans (Jira epics)

| Plan / Epic | Scope | Stories |
|---|---|---|
| CampusHub Platform | Identity, tenants, invites, billing, ops vs campus | CH-S01–S04 |
| CampusHub Teach & learn | Analytics, quizzes, assignments, notes, announcements, gradebook, due dates | CH-S07, CH-S11–S16 |
| CampusHub Discovery & engagement | Notifications, certificates, chat/AI, catalog search, progress, calendar | CH-S05–S06, CH-S08–S10, CH-S16 calendar is with due dates |

CH-S16 due dates lives in Teach & learn; calendar UI is on My learning.

## Stories

| Key | Story | Spec | Plan | Jira epic |
|---|---|---|---|---|
| CH-S01 | Campus signup, tenants, and plan gates | [003](../003-tenants-plans/spec.md) | [plan](../003-tenants-plans/plan.md) | Platform |
| CH-S02 | Invites and People | [004](../004-invites-people/spec.md) | [plan](../004-invites-people/plan.md) | Platform |
| CH-S03 | Mock billing | [005](../005-mock-billing/spec.md) | [plan](../005-mock-billing/plan.md) | Platform |
| CH-S04 | Platform Ops vs campus console | [006](../006-ops-campus/spec.md) | [plan](../006-ops-campus/plan.md) | Platform |
| CH-S05 | Notifications and SSE | [007](../007-notifications/spec.md) | [plan](../007-notifications/plan.md) | Discovery |
| CH-S06 | Course completion certificates | [008](../008-certificates/spec.md) | [plan](../008-certificates/plan.md) | Discovery |
| CH-S07 | Teacher course analytics | [009](../009-course-analytics/spec.md) | [plan](../009-course-analytics/plan.md) | Teach & learn |
| CH-S08 | Chat rooms and AI tutor | [010](../010-chat-ai-tutor/spec.md) | [plan](../010-chat-ai-tutor/plan.md) | Discovery |
| CH-S09 | Catalog filters, sort, recommended | [011](../011-catalog-discovery/spec.md) | [plan](../011-catalog-discovery/plan.md) | Discovery |
| CH-S10 | Student progress dashboard | [012](../012-progress-dashboard/spec.md) | [plan](../012-progress-dashboard/plan.md) | Discovery |
| CH-S11 | Course quizzes | [013](../013-quizzes/spec.md) | [plan](../013-quizzes/plan.md) | Teach & learn |
| CH-S12 | Course assignments | [014](../014-assignments/spec.md) | [plan](../014-assignments/plan.md) | Teach & learn |
| CH-S13 | Lecture notes | [015](../015-lecture-notes/spec.md) | [plan](../015-lecture-notes/plan.md) | Teach & learn |
| CH-S14 | Course announcements | [016](../016-announcements/spec.md) | [plan](../016-announcements/plan.md) | Teach & learn |
| CH-S15 | Course gradebook | [001](../001-course-gradebook/spec.md) | [plan](../001-course-gradebook/plan.md) | Teach & learn |
| CH-S16 | Assignment due dates and calendar | [002](../002-assignment-due-dates/spec.md) | [plan](../002-assignment-due-dates/plan.md) | Teach & learn |

## Upload to Jira

1. Import `jira-import.csv` (Issues → Import issues from CSV). Map **Epic Name**, **Issue Type**, **Status**, **Labels**, **Description**.
2. Or set `JIRA_BASE_URL`, `JIRA_EMAIL`, `JIRA_API_TOKEN`, `JIRA_PROJECT_KEY` and run:

```powershell
pwsh specs/jira/push-jira.ps1
```

Keys are written to `specs/jira/jira-keys.json`. Then create a Jira **Plan** with JQL `labels = campushub AND labels = sdd`.
