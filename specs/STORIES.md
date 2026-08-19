# CampusHub features — real product order

Every row is one **A–Z feature story**: start from **auth** (CH-S17), then that feature’s screens, APIs, mock, test, Done.

Gateway: `http://localhost:5000`. Password: `CampusHub!123`.  
Workflow: [WORKFLOW.md](WORKFLOW.md).

**Auth facts (all stories):** the browser holds cookie `campushub.bff` only. Access + refresh tokens live in that cookie. YARP sends `Authorization: Bearer`. Refresh is silent (`/connect/token`). Logout is session revoke (`/logout`). There is **no** public student self-register and **no** OAuth `/connect/revoke`.

## Order (as the app is wired)

| # | Story | Feature | Spec |
|---|---|---|---|
| 1 | **CH-S17** | Register, login, access token, refresh, logout | [017](017-auth-session/spec.md) |
| 2 | **CH-S18** | Account profile + password | [018](018-account-profile/spec.md) |
| 3 | **CH-S01** | Campus signup, tenants, plan gates | [003](003-tenants-plans/spec.md) |
| 4 | **CH-S02** | Invites and People (user register by invite) | [004](004-invites-people/spec.md) |
| 5 | **CH-S09** | Catalog browse, filters, sort, recommended | [011](011-catalog-discovery/spec.md) |
| 6 | **CH-S19** | Enroll, checkout, mock payment | [019](019-enroll-checkout/spec.md) |
| 7 | **CH-S20** | Course player and curriculum | [020](020-course-player/spec.md) |
| 8 | **CH-S13** | Lecture notes | [015](015-lecture-notes/spec.md) |
| 9 | **CH-S08** | Chat rooms and Ask AI | [010](010-chat-ai-tutor/spec.md) |
| 10 | **CH-S11** | Quizzes | [013](013-quizzes/spec.md) |
| 11 | **CH-S12** | Assignments | [014](014-assignments/spec.md) |
| 12 | **CH-S16** | Due dates + calendar | [002](002-assignment-due-dates/spec.md) |
| 13 | **CH-S14** | Announcements | [016](016-announcements/spec.md) |
| 14 | **CH-S15** | Gradebook | [001](001-course-gradebook/spec.md) |
| 15 | **CH-S10** | My learning dashboard | [012](012-progress-dashboard/spec.md) |
| 16 | **CH-S05** | Notifications + SSE | [007](007-notifications/spec.md) |
| 17 | **CH-S06** | Completion certificates | [008](008-certificates/spec.md) |
| 18 | **CH-S21** | Course pass QR + attendance | [021](021-course-pass/spec.md) |
| 19 | **CH-S07** | Teacher analytics | [009](009-course-analytics/spec.md) |
| 20 | **CH-S03** | Mock billing | [005](005-mock-billing/spec.md) |
| 21 | **CH-S04** | Ops `/ops` vs campus `/campus` | [006](006-ops-campus/spec.md) |

CH-S01–S16 map to Jira ideas MDP-12–MDP-27. CH-S17–S21 are specified in-repo until the next Jira push.

---

## 1. CH-S17 Auth — register, login, token, refresh, logout

**A–Z:** `/signup` (campus) → `/invite/:token` or `/ops/users` (user) → `/login` → Identity `/Account/Login` → cookie + access JWT + refresh → `/whoami` → silent refresh → `/logout`.

| | |
|---|---|
| Screens | `/signup`, `/login`, `http://localhost:5101/Account/Login`, `/logout`, `/whoami` |
| Apply | Gateway `Program.cs`, `AccessTokenRefresher`, Identity `AuthorizationController`, `Login.cshtml`, `session.ts`, `TokenRefreshPolicy` |
| Test | `dotnet test --filter Story=CH-S17` |
| Mock | `student@` / `teacher@` / `admin@` + `CampusHub!123` |
| Revoke | **Logout** ends BFF + Identity cookies. Not `/connect/revoke`. |

Full AC: [spec](017-auth-session/spec.md).

---

## 2. CH-S18 Account

**A–Z:** Login → `/account` → edit name → change password → still signed in.

---

## 3. CH-S01 Tenants / campus register / plans

**A–Z:** Anonymous `/signup` → login as owner → tenant + plan on `/whoami` → Free vs Campus gates Ask AI / chat / seats.

---

## 4. CH-S02 Invites / People

**A–Z:** Login as admin → `/people` → create invite → open `/invite/:token` → set password → login as new member.

---

## 5. CH-S09 Catalog

**A–Z:** Login → `/catalog` → search/filters/sort/recommended/wishlist. JWT required. Meilisearch optional.

---

## 6. CH-S19 Enroll / mock pay

**A–Z:** Login as student → course landing → `/enroll/:id` → Pay successfully / fail → saga → player unlocked. Payment API is internal, not on YARP.

---

## 7. CH-S20 Player / curriculum

**A–Z:** Login → enroll → `/learn/course/:id` → lectures complete. Other player tabs are stories 8–14.

---

## 8–14. Teach & learn (on the player + editor)

Each starts with **login as student or teacher** (CH-S17), then:

| Story | A–Z |
|---|---|
| CH-S13 Notes | Player Notes tab → persist → `/learn` snippets |
| CH-S08 Chat / Ask AI | `/chat` or player Ask; Free plan catalog-text; Campus may use model |
| CH-S11 Quizzes | Teacher editor → student Quiz tab → percent / pass |
| CH-S12 Assignments | Teacher create → student submit → teacher grade |
| CH-S16 Due dates | Teacher due → player overdue/late → `/learn` calendar |
| CH-S14 Announcements | Teacher post → player list |
| CH-S15 Grades | Teacher `/catalog/:id/gradebook` → student Grades tab (self only) |

---

## 15. CH-S10 My learning

**A–Z:** Login as student → `/learn` → streak, bars, continue, calendar, links.

---

## 16. CH-S05 Inbox

**A–Z:** Login → bell + `/learn/inbox` → SSE unread. Seeded welcome rows.

---

## 17. CH-S06 Certificates

**A–Z:** Login → complete all lectures → `/learn/certificates`.

---

## 18. CH-S21 Course pass / attendance

**A–Z:** Login → enroll → `/learn/pass` QR → teacher `/learn/attendance` scan. Enrollment cancel **revokes the pass** (credential, not OAuth token).

---

## 19. CH-S07 Analytics

**A–Z:** Login as teacher → `/catalog/mine` → `/catalog/:id/analytics`.

---

## 20. CH-S03 Billing

**A–Z:** Login as admin → `/billing` → mock upgrade → **sign in again** so JWT `plan` refreshes (CH-S17).

---

## 21. CH-S04 Ops vs campus

**A–Z:** Login as `admin@` → `/ops`. Campus owner → `/campus`. Platform-only filter.

---

Constants: `CampusHub.BuildingBlocks.Sdd.SddStories` and `sdd-stories.ts`.
