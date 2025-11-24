# StickyBoard Data Access Layer (DAL)

## Overview

The StickyBoard DAL uses a lightweight repository pattern on top of Npgsql. It avoids ORMs and provides full control over SQL, ensuring predictable performance, clear logic, and strict safety rules.

This document explains the architecture, soft deletion system, versioning, concurrency rules, and sync mechanisms.

------

## Core Architecture

```mermaid
classDiagram
    class RepositoryBase~T~ {
        -NpgsqlDataSource _db
        +Conn(ct)
        +GetByIdAsync()
        +GetAllAsync()
        +ExistsAsync()
        +CountAsync()
        +GetPagedAsync()
        +GetUpdatedSinceAsync()
        +DeleteAsync()
        <<abstract>>
        +CreateAsync(T)
        +UpdateAsync(T)
    }

    class IEntity {
        Guid Id
        DateTime UpdatedAt
    }

    class ISoftDeletable {
        DateTime? DeletedAt
    }

    class IVersionedEntity {
        int Version
    }

    class IAllowDeleted {
        bool IncludeDeleted
    }

    RepositoryBase~T~ --> IEntity
    RepositoryBase~T~ --> ISoftDeletable
    RepositoryBase~T~ --> IVersionedEntity
    RepositoryBase~T~ --> IAllowDeleted
```

------

## Table Name Resolution

Each entity resolves its table name in one of two ways:

- Explicit via `[Table("name")]`
- Implicit via lowercase type name

------

## Mapping System

The DAL uses a single mapping function:

```csharp
protected virtual T MapRow(NpgsqlDataReader r)
```

This ensures that all repositories share consistent mapping behavior.

```mermaid
flowchart LR
    A[Database Row] --> B[MappingHelper]
    B --> C[T Entity]
```

------

## Soft Delete System

Soft deletion is automatic if the entity implements `ISoftDeletable`.

Soft-delete behavior:

- `SELECT` queries automatically filter `deleted_at IS NULL`
- `DELETE` becomes `UPDATE table SET deleted_at = NOW()`

### Filtering Logic

```mermaid
flowchart TD
    A[Query SQL] --> B{T implements ISoftDeletable?}
    B -- No --> C[Return raw SQL]
    B -- Yes --> D{IAllowDeleted.IncludeDeleted?}
    D -- Yes --> C[Return raw SQL]
    D -- No --> E[Inject 'deleted_at IS NULL']
```

------

## Optimistic Concurrency

Versioned entities use:

- WHERE `id = @id AND version = @version`
- UPDATE increments version

This prevents stale overwrites during multi-device usage.

```mermaid
sequenceDiagram
    participant ClientA
    participant ClientB
    participant DB

    ClientA->>DB: UPDATE id=5 version=3
    DB-->>ClientA: OK version=4

    ClientB->>DB: UPDATE id=5 version=3
    DB-->>ClientB: 0 rows (conflict)
```

------

## Hard Delete vs Soft Delete

### Rules

- If `ISoftDeletable`: use soft delete
- Otherwise: hard delete
- Some repositories override delete entirely and forbid it

```mermaid
flowchart LR
    A[DeleteAsync] --> B{ISoftDeletable?}
    B -- Yes --> C[Soft Delete]
    B -- No --> D[Hard Delete]
```

------

## Paging System

Uses a single-query window function:

```
SELECT *, COUNT(*) OVER() AS total_count
FROM table
ORDER BY updated_at DESC
LIMIT @limit OFFSET @offset;
```

Provides:

- deterministic pagination
- only one round-trip to the DB

```mermaid
flowchart LR
    A[Client Request] --> B[Repo.GetPagedAsync]
    B --> C[(DB Query with Window Function)]
    C --> D[PagedResult]
```

------

## Sync Model (Delta Queries)

Two main methods:

- `GetUpdatedSinceAsync(DateTime since)`
- `GetUpdatedSincePagedAsync()`

```mermaid
flowchart TD
    A[Mobile Device] --> B[GET /sync?since=timestamp]
    B --> C[RepositoryBase]
    C --> D[(DB updated_at > @since)]
    D --> E[Changed Records]
```

------

## Security Principles

The DAL enforces:

- mandatory parameterized SQL
- no dynamic string concatenation
- strict domain rules via explicit overrides
- predictable delete safety via ISoftDeletable
- optimistic concurrency using version checks

------

## Error Prevention & Safety

### Protection Layers

1. **ORM-like safety without ORM bloat**
2. **Query injection protections** (params only)
3. **Soft delete by default**
4. **Version checks** to avoid data races
5. **Controlled deletes** in social/contact logic

```mermaid
flowchart TD
    A[Incoming Request] --> B[Repository]
    B --> C[Soft-delete Filter]
    B --> D[Concurrency Check]
    B --> E[Domain Rules]
    C --> F[Final SQL]
    D --> F
    E --> F
```

------

## Consistency Guarantees

The DAL guarantees:

- consistent table access patterns
- uniform SQL filtering
- deterministic mapping
- stable sync behavior across all devices
- secure and predictable lifecycle for deletable entities

------

# Summary

The StickyBoard DAL is a highly predictable architecture providing:

- Soft delete with override capability
- Optimistic concurrency
- Delta sync support for devices
- Strict domain rule enforcement
- SQL-level transparency and safety

It is efficient, secure, maintainable, and production-ready.