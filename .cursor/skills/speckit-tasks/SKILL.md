---
name: speckit-tasks
description: Break the active CampusHub spec and plan into tasks.md. Use when the user says tasks or the plan touches more than a few files.
---

# Tasks

## Steps

1. Read `spec.md` and `plan.md` in the active feature directory.
2. Copy `.specify/templates/tasks-template.md` to `tasks.md`.
3. One checkbox per file-level change. Tag `[USn]` and `[P]` when parallel-safe.
4. Foundation (schema/endpoints) before UI. Seed must not brick service startup.
5. Last tasks: smoke the success criteria, then stage related files only.
