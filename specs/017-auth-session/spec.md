# Feature Specification: Auth session — register, login, token, refresh, logout

**Story**: CH-S17  
**Jira idea**: (push with next Jira import)  
**Workflow**: Specify ✅ Apply ✅ Test ✅ Mock ✅ Retest ✅ **Done**  
**Created**: 2026-08-20  
**Status**: Implemented  
**Input**: Full A–Z identity as it exists in the running product (cookie BFF + OpenIddict). No public student self-register. No OAuth `/connect/revoke`.

## User story

**As a** campus user,  
**I want** to register through an approved path, sign in, keep a secure session, and sign out,  
**so that** only I can use my account and tokens never appear in the browser.

## Qualified header

| Field | Value |
|---|---|
| Persona | Anonymous visitor, campus owner, invitee, seeded student/teacher/admin |
| Value | A session so every other feature can call APIs |
| Screens | `/signup`, `/login`, Identity `/Account/Login`, `/logout`, `/whoami`, `/invite/:token`, `/ops/users` |
| Tests | `[Trait("Story", "CH-S17")]` `TokenRefreshPolicyTests` + screen smoke |
| Mock | Seeded `student@` / `teacher@` / `admin@` + `CampusHub!123` |

## A–Z (this feature, real order)

1. **Register a campus** — `/signup` creates tenant + Administrator, then redirects to login. Plan starts `free`.
2. **Register a user by invite** — admin copies `/invite/{token}`; invitee sets password (≥10, complexity). That is how students/teachers join. There is no public “sign up as student” without invite or ops.
3. **Register from Ops** — platform admin at `/ops/users` creates Student/Teacher/Admin (default password `CampusHub!123`).
4. **Login** — shell Sign in → `GET /login?returnUrl=` (gateway challenge) → Identity Razor `http://localhost:5101/Account/Login` (email, password, remember me) → OIDC code+PKCE callback `/signin-oidc`.
5. **Access token** — stored on the **BFF cookie** `campushub.bff` (`SaveTokens`). Browser never sees the JWT. YARP attaches `Authorization: Bearer` to `/api/*`.
6. **Refresh token** — also in the cookie (`offline_access`). Gateway `AccessTokenRefresher` POSTs `/connect/token` (`grant_type=refresh_token`) when expiry is within **2 minutes**. Failed refresh rejects the principal (session ends).
7. **Revoke / logout** — `GET/POST /logout` signs out the BFF cookie **and** OIDC end-session → Identity `/connect/logout` (clears `campushub.identity`). Product revoke is **session logout**, not OpenIddict token revocation. There is **no** `/connect/revoke`.
8. **Session JSON** — `GET /whoami` (cookie) returns name, email, sub, roles, tenant, plan. Angular `SessionService` uses this. API 401 → `/login`.
9. **Stay signed in** — Identity cookie Remember me; BFF cookie sliding expiration. Password change at `/account` does not kick other sessions.

## Screens

| Screen | URL | Actor | Must show |
|---|---|---|---|
| Campus register | `http://localhost:5000/signup` | anonymous | Create campus + owner |
| Gateway login start | `http://localhost:5000/login?returnUrl=/catalog` | anonymous | Redirect to Identity |
| Login form | `http://localhost:5101/Account/Login` | anonymous | Email/password; seeded accounts listed |
| OIDC callback | `http://localhost:5000/signin-oidc` | browser | Then catalog/shell |
| Who am I | `http://localhost:5000/whoami` | signed-in | JSON session |
| Logout | `http://localhost:5000/logout` | signed-in | Cookie gone; back to `/` |
| Invite register | `http://localhost:5000/invite/:token` | invitee | Set password, then login |
| Ops user create | `http://localhost:5000/ops/users` | platform admin | Create user |

## User Scenarios & Testing

### User Story 1 - Sign in with a seeded account (P1)

**Independent Test**: Open `http://localhost:5000`, Sign in, `student@campushub.local` / `CampusHub!123`, land on catalog. `/whoami` is authenticated.

**Acceptance Scenarios**:

1. **Given** seeded users, **When** they complete Identity login, **Then** `campushub.bff` is set and `/whoami` has `sub`, `role`, `tenant_id`, `plan`.
2. **Given** a signed-in user, **When** Angular calls `/api/catalog/courses`, **Then** the gateway attaches a Bearer access token (no JWT in JS).
3. **Given** Sign out, **When** they hit `/whoami` or a guarded route, **Then** they are anonymous and must login again.

### User Story 2 - Register then login (P1)

**Independent Test**: `/signup` a new campus; login as that owner; or accept an invite then login.

**Acceptance Scenarios**:

1. **Given** a new campus signup, **When** it succeeds, **Then** they are sent to `/login` and can sign in.
2. **Given** an invite token, **When** they set a valid password, **Then** they can login and appear on People.
3. **Given** password shorter than Identity policy (10 + digit + upper + symbol), **When** they register, **Then** it fails with a validation error.

### User Story 3 - Refresh and revoke session (P1)

**Independent Test**: Stay on the site until access token is near expiry (or unit-test the 2-minute policy). Click Sign out.

**Acceptance Scenarios**:

1. **Given** an access token expiring within 2 minutes and a refresh token, **When** the cookie is validated, **Then** gateway refreshes at `/connect/token` and renews the cookie.
2. **Given** refresh fails or is missing, **When** validation runs, **Then** the principal is rejected (must login).
3. **Given** `/logout`, **When** it completes, **Then** BFF and Identity cookies are gone. Downstream APIs must not accept the old browser cookie.

### Edge Cases

- Password grant is **not** enabled. Only authorization code, refresh, and client_credentials (enrollment service).
- `/api/*` unauthenticated returns **401**, not an OIDC redirect.
- Plan claim in JWT is stale until re-login after billing upgrade (CH-S03).
- Introspect `/connect/introspect` exists; the student UI does not call it.
- Credential “revoked” on a course pass (CH-S21) is **not** OAuth token revoke.

## Requirements

- **FR-001**: Browser MUST authenticate through the gateway BFF cookie, not a public JWT in localStorage.
- **FR-002**: Identity MUST issue access tokens (JWT) and refresh tokens (`offline_access`) to client `campushub-gateway`.
- **FR-003**: Gateway MUST attach the access token to proxied `/api` calls.
- **FR-004**: Gateway MUST refresh the access token when it expires within 2 minutes.
- **FR-005**: Logout MUST end the BFF session and Identity login session.
- **FR-006**: Register MUST be campus signup, invite accept, or ops create — not an open student registration page.
- **FR-007**: Seeded demo users MUST keep working with `CampusHub!123`.

## Success Criteria

- **SC-001**: Seeded student can sign in and browse `/catalog`.
- **SC-002**: Sign out requires a new login.
- **SC-003**: `dotnet test --filter Story=CH-S17` passes.

## Apply / Test / Mock / Done

- [x] Specify finished
- [x] Apply (CH-S17 on gateway, Identity, `SessionService`, `TokenRefreshPolicy`)
- [x] Test
- [x] Mock (Identity seeder)
- [x] Retest
- [x] Done

## Assumptions

- Out of scope unless a new spec says so: OpenIddict `/connect/revoke`, password grant, SPA-held refresh tokens, social login, email verification.
