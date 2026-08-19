# Implementation Plan: [FEATURE]

**Spec**: `specs/[###-feature-name]/spec.md`
**Date**: [DATE]

## Summary

[What we will build and where it lives]

## Technical Context

**Language/Version**: .NET 9 (Catalog/other APIs), Angular (shell + MFEs), Node (chat only if in scope)
**Storage**: SQLite per service unless spec says otherwise
**Edge**: YARP gateway `:5000`
**UI**: catalog-mfe and/or learning-mfe

## Constitution Check

- [ ] Spec exists and is not Draft-only if we are implementing
- [ ] One vertical slice; owning service is named
- [ ] No new public port; gateway still the edge
- [ ] Seed/schema cannot brick Catalog/Identity startup
- [ ] Commit set will exclude db/bin/tmp

## Files likely to change

- `src/services/...`
- `src/frontend/projects/...`

## Research / risks

- [SQLite FK, Guid TEXT, DateTimeOffset ORDER BY, paged `{ items }` APIs]
