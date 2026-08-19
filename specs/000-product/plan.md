# Implementation Plan: CampusHub product (Jira)

**Spec**: `specs/000-product/spec.md`
**Date**: 2026-08-19

## Summary

Three Jira plans (epics) cover every shipped slice. Stories CH-S01–S16 are Done. New work starts as `specs/NNN-slug` then a Jira story in the matching epic.

## Plans

1. **CampusHub Platform** — Identity, tenants, invites, billing, `/ops` vs `/campus`
2. **CampusHub Teach & learn** — quizzes, assignments, notes, announcements, gradebook, due dates
3. **CampusHub Discovery & engagement** — search, progress, notifications, certificates, chat/AI

## Constitution Check

- [x] Spec before code for new slices
- [x] Gateway remains the only public edge
- [x] One owning service per story
