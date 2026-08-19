# CampusHub Constitution

Non-negotiable rules for every spec, plan, and implementation. If a slice conflicts with this file, change the spec or amend the constitution — do not silently ignore it.

## Core Principles

### Spec before code

New product slices start with `specs/NNN-slug/spec.md`. Do not implement a slice from a chat prompt alone. "Proceed to next" means: pick or write the next spec, then plan and implement that spec.

### One vertical slice

Each spec is one demoable capability (API + UI the user can click). Do not mix unrelated services in the same slice. Prefer Catalog/learning features that follow existing endpoint + Angular MFE patterns unless the spec clearly belongs to Enrollment, Identity, Chat, Access, or Notification.

### Gateway is the only public edge

Browsers talk to `http://localhost:5000`. Downstream APIs stay on 510x. Do not expose a microservice port as the student/teacher UI. Session is the BFF cookie; YARP attaches JWTs.

### Own data in the owning service

Catalog owns courses, curriculum, quizzes, assignments, notes, announcements, gradebook, wishlist, and Ask AI. Enrollment owns the payment saga. Identity owns users, tenants, invites, plans. Access owns QR passes and certificates. Notification owns inbox/SSE. Chat owns rooms and messages. Do not duplicate another service's source of truth.

### Keep local persistence boring

SQLite (or JSON for chat) is fine until a spec requires otherwise. Schema changes use `Ensure`/`CREATE TABLE IF NOT EXISTS` with TEXT Guid ids. New catalog tables must not take down seed on FK or DateTimeOffset ORDER BY failures — wrap seed in try/catch and keep the API startable.

### Small, related commits

Stage only files for the slice. Never commit `.tmp-build/`, `*.db`, `bin/`, `obj/`, or `node_modules/`. Default password in docs stays `CampusHub!123`.

## Development Constraints

- Angular: catalog-mfe for browse/teach/edit; learning-mfe for player, progress, certificates, inbox.
- Teacher-only routes use `teacherGuard`. Student features must work when enrolled; staff may bypass.
- Plan gates stay real: Free vs Campus (for example Ask AI model vs catalog text).
- Meilisearch and AI keys are optional; SQL and catalog-text fallbacks must keep working.
- Do not introduce RabbitMQ, Mongo, or extra containers unless the spec names them.

## Governance

- Amend this constitution in the same change that violates it, with a one-line reason in the spec's Constitution Check.
- Specs are the review surface: if it is not in `spec.md`, it is not required.
- After implement, mark tasks done and set spec status to Implemented.

**Version**: 1.0.0 | **Ratified**: 2026-08-19 | **Last Amended**: 2026-08-19
