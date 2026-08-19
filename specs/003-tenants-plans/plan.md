# Implementation Plan: Tenants, campus signup, and plan gates

**Spec**: `specs/003-tenants-plans/spec.md`  
**Story**: CH-S01  
**Status**: Implemented

## Summary

Identity owns tenants and plans. Shell signup and session expose plan. Catalog Ask AI respects Free vs Campus.

## Technical Context

**Owning service**: Identity (+ shell signup, Catalog tutor gate)  
## Code to apply

| Area | Path |
|---|---|
| Identity | `CampusEndpoints.cs` tenants |
| UI | `src/frontend/projects/shell/src/app/signup.ts` |
| Gate | Catalog Ask AI respects Free vs Campus (`CourseTutor`) |

## Test / Mock

`/signup` then seeded users still login with `CampusHub!123`.
