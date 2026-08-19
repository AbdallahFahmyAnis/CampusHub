# Qualified story workflow (SDD)

Every CampusHub story moves through the same stages. Specs, plans, code comments, and tests must name the **story id** (`CH-Snn`) and the spec folder.

## Stages

| Stage | Meaning | Status label | Exit criteria |
|---|---|---|---|
| **Specify** | Finish `spec.md` (persona, screens, AC, mocks) | Spec finished | Reviewer can demo from the spec alone |
| **Apply** | Implement the plan’s file list | In progress | Code comments / types cite `CH-Snn` |
| **Test** | Automated tests + screen smoke | Testing | `dotnet test` traits for the story pass; screens checked |
| **Mock** | Seed / demo data so the story is clickable | Mock in progress | Seeded users/courses show the screen without extra setup |
| **Retest** | Repeat tests against mock data | Testing | Smoke on seeded Algorithms / Linear Algebra |
| **Done** | Slice is shipped | Done | Spec status Implemented; Jira idea/work item Done |

Do not skip **Specify**. Do not mark **Done** without **Test** and a **Mock** path.

Jira Product Discovery (**MDP**) holds ideas. Work Management (**CHUB**) holds executable tasks. Move CHUB tasks: To Do → In Progress (Apply) → Testing → Done. Use the description checklist for Mock.

## Code pattern

```csharp
/// <summary>SDD CH-S16 / MDP-27 — specs/002-assignment-due-dates.</summary>
```

Angular files use the same ids in a file header comment. Tests use `[Trait("Story", "CH-S16")]`.
