# Implementation Plan: Account profile

**Spec**: `specs/018-account-profile/spec.md`  
**Story**: CH-S18  
**Status**: Implemented

## Code to apply

| Area | Path |
|---|---|
| UI | `src/frontend/projects/shell/src/app/profile.ts` |
| Gateway | `AccountEndpoints.cs` `GET/PUT /api/account/me`, `POST /api/account/password` |
| Identity | `UserEndpoints.cs` / `MeController.cs` |
