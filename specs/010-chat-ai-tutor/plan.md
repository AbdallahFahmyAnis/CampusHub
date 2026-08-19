# Implementation Plan: Chat rooms and AI tutor

**Spec**: `specs/010-chat-ai-tutor/spec.md`  
**Story**: CH-S08  
**Status**: Implemented

## Summary

Node Chat `:5107`. Catalog `CourseTutor` + `/ask`. Player Ask AI tab. Plan gate in session.

## Code to apply

| Area | Path |
|---|---|
| Chat | Node chat + `chat.ts` `/chat` |
| Catalog | `POST /courses/{id}/ask`, `CourseTutor.cs` |
| UI | player Ask AI tab |

