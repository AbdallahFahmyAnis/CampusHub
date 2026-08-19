# Implementation Plan: Notifications and SSE

**Spec**: `specs/007-notifications/spec.md`  
**Story**: CH-S05  
**Status**: Implemented

## Summary

Notification.Api stores messages. Gateway proxies `/api/notifications`. Learning inbox + shell bell + SSE.

## Code to apply

| Area | Path |
|---|---|
| API | `NotificationEndpoints.cs` |
| UI | `inbox.ts`, shell bell |

