# Implementation Plan: Mock billing

**Spec**: `specs/005-mock-billing/spec.md`  
**Story**: CH-S03  
**Status**: Implemented

## Summary

Identity/plan APIs plus Angular billing page. Payment service remains a mock PSP for enrollments.

## Code to apply

| Area | Path |
|---|---|
| API | Identity billing GET/upgrade, Gateway `/campus` billing |
| UI | `src/frontend/projects/shell/src/app/billing.ts` |

