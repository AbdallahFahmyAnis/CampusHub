# CampusHub qualified stories

Workflow: [WORKFLOW.md](WORKFLOW.md). Jira ideas: MDP-12–MDP-27. Demo password: `CampusHub!123`. Gateway: `http://localhost:5000`.

Seeded: `student@campushub.local`, `teacher@campushub.local`, `admin@campushub.local`.  
Courses: Algorithms `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1`, Linear Algebra `…bbb2`.

All CH-S01–CH-S16 are **Specify → Apply → Test → Mock → Retest → Done**.

| Story | Idea | Screens (gateway) | Apply (code) | Test | Mock |
|---|---|---|---|---|---|
| CH-S01 | MDP-12 | `/signup` | `CampusEndpoints.cs`, `signup.ts` | Manual signup + plan gate | Seeded campus tenant |
| CH-S02 | MDP-13 | `/people`, `/invite/:token` | invites in Identity, `people.ts` | Invite + accept | Admin can invite |
| CH-S03 | MDP-14 | `/billing` | billing APIs, `billing.ts` | Upgrade mock plan | Seeded invoices |
| CH-S04 | MDP-15 | `/ops`, `/campus` | Gateway ops, `campus-dashboard.ts` | Two consoles | Admin vs campus |
| CH-S05 | MDP-16 | `/learn/inbox` | `NotificationEndpoints.cs`, `inbox.ts` | Bell + inbox | Seeded inbox rows |
| CH-S06 | MDP-17 | `/learn/certificates` | `AccessEndpoints.cs`, `certificates.ts` | Cert after complete | Seeded creds if any |
| CH-S07 | MDP-18 | `/catalog/:id/analytics` | `CourseLearningEndpoints` stats, `course-analytics.ts` | Teacher stats | Published courses |
| CH-S08 | MDP-19 | `/chat`, player Ask AI | Chat + `AskCourse`, `chat.ts`, player | Chat + catalog-text ask | `tutor:{courseId}` |
| CH-S09 | MDP-20 | `/catalog` | `CatalogEndpoints` list, `course-list.ts` | Filters/SQL fallback | Seeded catalog |
| CH-S10 | MDP-21 | `/learn` | progress dashboard API, `progress-dashboard.ts` | Streak/bars | Student lecture progress |
| CH-S11 | MDP-22 | editor + player Quiz | `QuizEndpoints.cs` | `QuizScoringTests` | Algorithms checkpoint |
| CH-S12 | MDP-23 | editor + player Assignments | `AssignmentEndpoints.cs` | Submit/grade smoke | Linear Algebra write-up |
| CH-S13 | MDP-24 | player Notes, `/learn` | `NoteEndpoints.cs` | Persist after refresh | Student notes |
| CH-S14 | MDP-25 | editor + player Announcements | `AnnouncementEndpoints.cs` | Post/list | Seeded posts |
| CH-S15 | MDP-26 | `/catalog/:id/gradebook`, Grades tab | `GradeEndpoints.cs`, `course-gradebook.ts` | Roster vs self | Seeded attempts |
| CH-S16 | MDP-27 | editor due, player due, `/learn` calendar | `AssignmentDueRules`, calendar GET | `AssignmentDueRulesTests` | Due in 5 days seed |

Constants in code: `CampusHub.BuildingBlocks.Sdd.SddStories` and `src/frontend/projects/catalog-mfe/src/app/sdd-stories.ts`.
