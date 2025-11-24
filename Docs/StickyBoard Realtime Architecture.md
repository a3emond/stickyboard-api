# StickyBoard Realtime Architecture

## Overview

StickyBoard implements *two completely different realtime mechanisms*, each responsible for a distinct class of data. These two channels operate independently but complement each other to deliver collaborative multi‑client synchronization and user‑level notifications.

The design separates:

- **Database-synchronized realtime (Delta Sync) — server‑authoritative**
- **User-targeted notifications & presence — app‑level bus**

This document describes each subsystem in technical depth, how they integrate, and where they are used inside the API.

------

# 1. Database-Backed Realtime (Delta Sync)

## Purpose

Propagate **all structural or content changes** that affect collaborative state:

- workspaces
- boards
- views
- cards
- comments
- messages
- attachments
- invites
- user-contact changes
- notifications table changes

Everything stored in PostgreSQL that must be synchronized across devices uses this pipeline.

## Architecture

### 1. PostgreSQL Triggers → Event Outbox

Each table has an OUTBOX trigger which writes a compact event row into:

```
event_outbox(cursor, topic, entity_id, workspace_id, board_id, op, payload)
```

Events appear for:

- INSERT (op = upsert)
- UPDATE (op = upsert)
- DELETE (op = delete)

### 2. Worker → Firebase Fan‑Out

A background worker continuously reads new `event_outbox` rows in cursor order. For each event:

- Resolve affected users (based on workspace/board membership)
- Publish to Firebase RTDB or FCM topic
- Update `sync_cursors` so clients know where to resume

### 3. Clients Apply Deltas

Clients subscribe to:

```
/workspaces/{id}/events
/boards/{id}/events
/inbox/{id}/events
```

Each event contains minimal JSON describing the changed entity.

### 4. Offline Model

Clients maintain:

- SQLite cache
- local mutation queue

When online, events are applied incrementally to the local cache.

### Characteristics

- **Authoritative**: Canonical state is PostgreSQL.
- **Deterministic**: Cursor-based ordering ensures no race conditions.
- **Low-bandwidth**: Only changed columns propagated.
- **Endpoints do not push data**: Controllers write to DB; DB triggers emit events.

### When to use this channel

Use this for **synchronizing state**, never for alerting a user.
 Examples:

- "A card title changed"
- "A comment was added"
- "A board was archived"
- "A list was moved"
- "A card was assigned — structural change only"

------

# 2. Application-Level Realtime (Notification Bus)

## Purpose

Deliver **user‑directed signals** that involve attention, alerts, or presence.
 This is not about updating shared state but informing a specific user.

## Architecture

Your API defines the interface:

```
INotificationBus
```

with methods such as:

- NotifyMentionAsync
- NotifyReplyAsync
- NotifyAssignmentAsync
- NotifyDirectMessageAsync
- NotifyInviteAsync
- PushAsync
- NotifyPresenceAsync

## Transport Layer

Initial implementation: `NoOpNotificationBus`.
 Future real implementation: Firebase FCM / RTDB or any other push provider.

### How it Works

1. A service performs a domain action.
2. The service calls a method of `INotificationBus`.
3. Implementation (future) sends:
   - mobile push (FCM/APNS), or
   - a Firebase presence update, or
   - a direct websocket event, depending on provider.

### Not DB-Driven

NotificationBus is completely separate from DB triggers.

### When to use this channel

Use this for **user‑targeted alerts**, not structural sync.
 Examples:

- "You were mentioned in a comment"
- "Someone replied to your message"
- "You received a direct message"
- "Your workspace invite was accepted"
- "Card assignment changed — notify the assignee"
- "Someone is typing in chat"
- "User came online/offline"

This is a **business-layer decision**, triggered explicitly by services.

------

# 3. Dual Realtime Model Summary

| Feature                                     | DB Outbox Sync                    | Notification Bus        |
| ------------------------------------------- | --------------------------------- | ----------------------- |
| Structural updates (cards, comments, views) | Yes                               | No                      |
| Board layout changes                        | Yes                               | No                      |
| Mentions                                    | No                                | Yes                     |
| Replies                                     | No                                | Yes                     |
| Direct message                              | No (sync of DM lists uses outbox) | Yes (alert)             |
| Assignment change                           | Yes (card update)                 | Yes (alert to assignee) |
| File uploads                                | Yes                               | Optional (push)         |
| Presence events                             | No                                | Yes                     |
| Invite events                               | Both                              | Yes                     |
| Offline sync                                | Yes                               | No                      |

------

# 4. How Services Should Use Both

### All services always:

- Write to DB via repositories
- DB triggers emit sync events automatically

### Some services additionally:

- Call INotificationBus to alert involved users

### Example: CommentService

On new comment:

1. Insert comment → triggers emit to outbox → devices update UI
2. Extract mentions → `NotifyMentionAsync`
3. If reply → `NotifyReplyAsync`
4. If in chat → optional presence typing notifications

### Example: CardService

On assignment change:

1. Update card.assignee → triggers emit to outbox
2. `NotifyAssignmentAsync(newAssigneeId, cardId)`

Nothing else required.
 You never manually publish structural realtime events — DB does it.

------

# 5. Current State vs Future Implementation

You already have:

- Full event_outbox pipeline in SQL
- Full schema support
- `INotificationBus` interface
- `NoOpNotificationBus` placeholder

Later you add:

- Worker to push outbox events to Firebase
- NotificationBus implementation using:
  - Firebase FCM (push)
  - Firebase RTDB (presence, typing)

No changes required in existing services.

------

# 6. Future Upgrade Paths

### A. Add Firebase Push

Implement an `FirebaseNotificationBus` that:

- maps methods to FCM payloads
- uses push_tokens table

### B. Add RTDB presence

Tie NotifyPresenceAsync to updates in:

```
/realtime/workspaces/{id}/presence/{userId}
```

### C. Add Web Push for desktop

Extend `PushAsync` with webpush provider.

### D. Add websocket session tracking

Table `ws_sessions` is already present.

------

# 7. Final Principles

1. **Delta Sync = DB-driven; structural; authoritative.**
2. **Notification Bus = service-driven; user-targeted; attentional.**
3. **Both exist simultaneously and never overlap responsibilities.**
4. **Service code must never manually emit data-sync events.**
5. **NotificationBus must never produce state changes, only signals.**

------

# End of Document