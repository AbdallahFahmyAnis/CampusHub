---
name: speckit-specify
description: Write or update a CampusHub feature spec under specs/NNN-slug/spec.md. Use when starting a slice, the user says specify, SDD, new feature, or proceed to next before coding.
---

# Specify

Create the **what and why**, not the stack.

## Steps

1. Read `.specify/memory/constitution.md` and `specs/000-product/spec.md`.
2. Choose the next number: look at existing `specs/NNN-*` folders; increment.
3. Copy `.specify/templates/spec-template.md` to `specs/NNN-slug/spec.md`.
4. Fill user stories (P1 first), functional requirements, success criteria. No endpoint paths unless needed for brownfield alignment.
5. Mark Status `Draft`. Set `.specify/feature.json` to `{ "feature_directory": "specs/NNN-slug" }`.
6. Stop for the user if anything is `NEEDS CLARIFICATION`; otherwise continue to plan when they asked to build it.

## Do not

Implement code in this skill. Do not skip the spec because the slice looks small.
