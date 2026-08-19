# Implementation Plan: Auth session

**Spec**: `specs/017-auth-session/spec.md`  
**Story**: CH-S17  
**Workflow**: Specify ✅ → Apply ✅ → Test ✅ → Mock ✅ → **Done** (brownfield)

## Summary

OpenIddict on Identity + cookie BFF on Gateway. Angular never stores tokens. Refresh is silent on cookie validation. Logout is session revoke.

## Code to apply

| Area | Path |
|---|---|
| Login/logout/whoami | `src/gateway/CampusHub.Gateway/Program.cs` |
| Refresh | `AccessTokenRefresher.cs`, `TokenRefreshPolicy.cs` |
| Authorize / end session | `Identity.Api/Controllers/AuthorizationController.cs` |
| Login UI | `Identity.Api/Pages/Account/Login.cshtml` |
| OpenIddict | `Identity.Api/DependencyInjection.cs` (`/connect/token`, refresh flow) |
| Shell | `session.ts`, `auth.guard.ts`, `app.config.ts` 401 → `/login` |
| Tests | `tests/CampusHub.Catalog.Api.Tests/TokenRefreshPolicyTests.cs` |

## Test

`dotnet test --filter Story=CH-S17`

Screen: Sign in `student@campushub.local` / `CampusHub!123`; `/whoami`; Sign out.

## Mock

Identity seeder: three users, tenant CampusHub Demo, client `campushub-gateway`.
