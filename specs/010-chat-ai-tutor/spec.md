# Feature Specification: Chat rooms and AI tutor

**Story**: CH-S08  
**Status**: Implemented  
**Created**: 2026-08-19

## User Story 1 - Course chat and Ask AI (Priority: P1)

Students join a course chat room. Ask AI in the player answers from course materials; Campus plan may use a model.

**Independent Test**: Open chat for a course; player Ask AI tab with and without `Ai__ApiKey`.

**Acceptance Scenarios**:

1. **Given** an enrolled student, **When** they open course chat, **Then** they can send messages.
2. **Given** no AI key, **When** they ask, **Then** the answer is from catalog text.
3. **Given** Free plan, **When** they ask, **Then** model AI is not required.

## Requirements

- **FR-001**: Chat service MUST own rooms and history.
- **FR-002**: Catalog MUST own Ask AI.

## Success Criteria

- **SC-001**: Ask AI works if Meilisearch/AI are down.
