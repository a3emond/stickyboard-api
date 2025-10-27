# StickyBoard – Data Layer Developer Reference

![PlantUML diagram](https://cdn-0.plantuml.com/plantuml/png/hLXVRzis47_tfn0oO7iO2cnedmfigE8egLSdzYIx1VOIKD95PasH0aavTOS1-mxxXliaEqhC68kgbCJsYwNxSxm_FnvFVASqaReksKHJpI18DCss-tD1P1xy-_S_mGNH99YJFSXWVJqF5ZkGEmhtayaFmHLma8G5fAg0IC8pcAWjvHK-bq76Y5AXrfAcjOPP9dX2lqBmqsH3SmWoAZC6xNC9UYDozKYt8jcM593eFJea8TE45ymp7lmv2V1tZiA553NFfu896NpQK0qD9vHItBV4xzwyUTDA2vc3jCJNhrzF_ZAH_FBY7oglaoXEWk-3SBrwVvmj6tTmjbOWrLaRe0tMa2p1_7wSvS7ReAvfxaXHIIX3geOlsb4lgAeOsT-G4ZnUk2EQoBMa7cSfuKuXzRCIF7NagGIY8G-rKQ0bA4tAIdzr0kig_wO0fjh54zPwqut9lFV4jKJvUwpRUw8sdgEnO60qKFLMuhFZp8dISr5GFkPA37SIr6Obji0VkzCGl4K778s-L1GjVJixCUpGfb5CXM2-9yPk38neAlWJ4dCfAV2yXv5eecjJFccDtQg6ObV4DPlKd_ry5hnwzQlvT_Lu6n1yx0YKXE_F3XE58Pf_PILR2ITy40iAtpM-ggDtsoA-2czcVuIhsU8cET5sMyY2SFgrSRMpxHwpHhPVxmvRtFDkwMp5VVlNKAQ76mAvJdSG3Mud25dOZU1gxPVo82vKkiT2FgPvyfwVg5Ks3wZeCusgMVVeUfylmlZ2jDTb55_FakI4PMyEkKwz6zfmeVsQ3bPhIIHmtTZjuQwePZvgFb95KygetfjMOLHlT_-J7Yble0I_ISSr0-N8NSWjGPPKgQUr6EjtFzYiRvs-UoK9xpy2B1uFS6Q0qYpImDIzFe2oVUcWqOKmSO-dmrmIJKrxCQXRXUUdWbi2MTDWZ5MITd1dcIF8UnwAba9HkxyfVqJ7QKxK-Xeq-JP8qCCHNv9CLCsQ9BrLvddSJHlaS02UerF1xsZXmTmBpyBB8iQ0mQutiV-6PW8x6XzJsv9MuTMfm355drja51raJ0SQqilpJT8-whwfQ6RsjCPdRw2mF3TC35KFwwQ1wo6C5SyyKfswJKp7X27rHbiC7C5pohSZVbnAKHhRlcNHIuw1sLmoD6vpQV5vW4QZFMbC-0CKUeq6TfGyH8g2NYjilNU3djhGIXqFyFawMKNnxEOA8Hclvz79OCvgfK5INXZ3DOCEs2t_XNYtsWymRjywI3QBNi2TtqxmpAq96-wAHlc5lk6QbDj-rLqwuYHbxhEWfvFPo1obSUNeEeTupdsShMOdgs-8qDvXbDswMlh-fONj9wDL8Vlf5WF4hji1mPBicI2vguIVyGUF9rEFScpMBsUueH3_2nnfJ_eTSqaPA6Eu0jdtCMFupvXjZwXCeeL4bpx0tZiM1dUOStt4pmlOqGoGyTHFSwxRQHguzl1WkNtfg2oGt293-74ph4brYoRtEZruEnP5dnRnXoWEVbz7w-X4-Faav1Ra8MPQsczYR1n4YG-KDoaHwUsMSkywmhBiy32KIykGL4WiItTyZMi-Uhtat0wr1XJnl4zfSM0LHNjnJEJWVTTYwxgs9KLI2caXC-3O1P2SWTOWnpFxd7pRco_V2uDFLyt3CSBBbvCqKdrU7mtWwoX9mgjsnfZTu35qibFe6fGYMDxY4CejUQm39i3p_liLo83kUbb9dNw6J7jzubpaVWEq7QLilUaVj1tRRXSFHpE-e_h5iVXNnNwJ7xxOiDUZdeqmow3IRfWPlHprUwUj4yefz5yvXeqP-u5IDIZNNot7lrcmz7TefoxPVm00)

```text
/StickyBoard.Api
├── Models/
│   ├── Base/
│   │   ├── IEntity.cs
│   │   └── BaseEntity.cs
│   ├── Users/
│   │   ├── User.cs
│   │   ├── AuthUser.cs
│   │   ├── RefreshToken.cs
│   │   └── UserRelation.cs
│   ├── Organizations/
│   │   ├── Organization.cs
│   │   └── OrganizationMember.cs
│   ├── Boards/
│   │   ├── Board.cs
│   │   ├── Permission.cs
│   │   └── Section.cs
│   ├── Tabs/
│   │   └── Tab.cs
│   ├── Cards/
│   │   ├── Card.cs
│   │   ├── Tag.cs
│   │   ├── CardTag.cs
│   │   └── Link.cs
│   ├── Clustering/
│   │   ├── Cluster.cs
│   │   └── Rule.cs
│   ├── Activities/
│   │   └── Activity.cs
│   ├── FilesAndOps/
│   │   ├── File.cs
│   │   └── Operation.cs
│   ├── Worker/
│   │   ├── WorkerJob.cs
│   │   ├── WorkerJobAttempt.cs
│   │   └── WorkerJobDeadletter.cs
│   ├── Messaging/
│   │   ├── Message.cs
│   │   └── Invite.cs
│   ├── Enums/
│   │   └── Enums.cs
│   └── Common/
│       ├── JsonDocumentExtensions.cs
│       └── MappingHelper.cs

├── Repositories/
│   ├── Base/
│   │   ├── RepositoryBase.cs
│   │   └── IRepository.cs
│   ├── Users/
│   │   ├── UserRepository.cs
│   │   ├── AuthUserRepository.cs
│   │   ├── RefreshTokenRepository.cs
│   │   └── UserRelationRepository.cs
│   ├── Organizations/
│   │   ├── OrganizationRepository.cs
│   │   └── OrganizationMemberRepository.cs
│   ├── Boards/
│   │   ├── BoardRepository.cs
│   │   └── PermissionRepository.cs
│   ├── SectionsAndTabs/
│   │   ├── SectionRepository.cs
│   │   └── TabRepository.cs
│   ├── Cards/
│   │   ├── CardRepository.cs
│   │   ├── TagRepository.cs
│   │   ├── CardTagRepository.cs
│   │   └── LinkRepository.cs
│   ├── Clustering/
│   │   ├── ClusterRepository.cs
│   │   └── RuleRepository.cs
│   ├── Activities/
│   │   └── ActivityRepository.cs
│   ├── FilesAndOps/
│   │   ├── FileRepository.cs
│   │   └── OperationRepository.cs
│   ├── Worker/
│   │   ├── WorkerJobRepository.cs
│   │   ├── WorkerJobAttemptRepository.cs
│   │   └── WorkerJobDeadletterRepository.cs
│   ├── Messaging/
│   │   ├── MessageRepository.cs
│   │   └── InviteRepository.cs
│   └── Repositories.csproj (if separated later)

```



**Last Updated:** October 2025
 **Scope:** Repository, Model, and Schema Architecture Reference

This document describes the **StickyBoard API data layer** for developers.
 It explains structure, conventions, and capabilities — not implementation details.
 Use this as a quick reference when extending models, repositories, or schema.

------

## 1. Overview

The StickyBoard data layer provides a **strongly typed abstraction** over the PostgreSQL database.
 It is built on:

- **PostgreSQL 17** (with JSONB, triggers, and enums)
- **Npgsql 9.x** (async I/O, token support)
- **C# 13 / .NET 9**
- **Repository pattern** with clear separation from business logic.

### Layer Flow

```
Controller → Service → Repository → Database
```

Repositories encapsulate **I/O logic only** — no business rules.
 Each service composes one or more repositories to form complete use cases.

------

## 2. Core Architectural Concepts

| Concept               | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Entity Models**     | Plain C# classes in `StickyBoard.Api.Models.*` mapped to SQL tables. |
| **Repositories**      | Handle all `INSERT`, `UPDATE`, `DELETE`, and query logic.    |
| **Enums**             | Represent PostgreSQL enum types and are mapped via `MapEnum<T>()`. |
| **CancellationToken** | Every async I/O method supports cooperative cancellation.    |
| **JSONB Fields**      | Stored as `JsonDocument` for flexibility and schema-free metadata. |
| **MappingHelper**     | Utility for dynamically mapping Npgsql results to model properties. |

------

## 3. Enum Mapping and PostgreSQL Integration

All database enum types are mapped in `Program.cs` using `NpgsqlDataSourceBuilder`:

```csharp
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<JobStatus>("job_status");
```

This allows:

- Type-safe reading and writing of enums.
- Seamless conversion between C# enums and PostgreSQL text values.

C# identifiers may safely use `_` or `@` to avoid reserved keywords (`private_`, `@event`).
 Npgsql automatically normalizes enum values to lowercase.

------

## 4. Models

### Naming Conventions

| Rule                                                    | Example                               |
| ------------------------------------------------------- | ------------------------------------- |
| Each table maps to one C# class in the matching folder. | `boards` → `Board.cs`                 |
| Namespace matches logical domain.                       | `StickyBoard.Api.Models.Cards`        |
| Columns map directly to public properties.              | `id → Id`, `created_at → CreatedAt`   |
| JSONB columns are typed as `JsonDocument`.              | `theme`, `rules`, `layout_meta`, etc. |
| Timestamps use UTC `DateTime`.                          | `created_at`, `updated_at`            |

### Base Interfaces

```csharp
public interface IEntity
{
    Guid? GetId() => null;
    DateTime? CreatedAt => null;
}

public interface IEntityUpdatable : IEntity
{
    DateTime UpdatedAt { get; set; }
}

```

Every model implements `IEntity` for generic repository compatibility.

------

## 5. Repository Pattern

All repositories inherit from `RepositoryBase<T>` and follow a unified contract.

```csharp
public abstract class RepositoryBase<T> : IRepository<T> where T : class, IEntity, new()
{
    public abstract Task<Guid> CreateAsync(T entity, CancellationToken ct);
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct);
    public abstract Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
```

### Key Characteristics

| Feature               | Description                                                |
| --------------------- | ---------------------------------------------------------- |
| **Async / Await**     | Every method is fully asynchronous.                        |
| **Token-Aware**       | All database calls accept `CancellationToken`.             |
| **Connection Safety** | Uses pooled `NpgsqlDataSource` connections.                |
| **Explicit SQL**      | All queries are manually defined (no ORM).                 |
| **Return Type**       | CRUD operations return the affected ID or boolean success. |

### Example Usage

```csharp
var boardId = await _boards.CreateAsync(board, ct);
var sections = await _sections.GetByBoardAsync(boardId, ct);
```

------

## 6. Repository Naming and Structure

| Domain             | Namespace                      | Example Repositories                                         |
| ------------------ | ------------------------------ | ------------------------------------------------------------ |
| Users & Auth       | `Repositories.Users`           | `UserRepository`, `AuthUserRepository`, `RefreshTokenRepository` |
| Organizations      | `Repositories.Organizations`   | `OrganizationRepository`, `OrganizationMemberRepository`     |
| Boards             | `Repositories.Boards`          | `BoardRepository`, `PermissionRepository`                    |
| Sections & Tabs    | `Repositories.SectionsAndTabs` | `SectionRepository`, `TabRepository`                         |
| Cards & Tags       | `Repositories.Cards`           | `CardRepository`, `TagRepository`, `CardTagRepository`, `LinkRepository` |
| Clustering & Rules | `Repositories.Clustering`      | `ClusterRepository`, `RuleRepository`                        |
| Activities         | `Repositories.Activities`      | `ActivityRepository`                                         |
| Files & Operations | `Repositories.FilesAndOps`     | `FileRepository`, `OperationRepository`                      |
| Worker Queue       | `Repositories.Worker`          | `WorkerJobRepository`, `WorkerJobAttemptRepository`, `WorkerJobDeadletterRepository` |
| Messaging & Social | `Repositories.Messaging`       | `MessageRepository`, `InviteRepository`                      |

------

## 7. Repository Capabilities by Category

### Users & Auth

- Create and manage user accounts and auth credentials.
- Fetch users by email or ID.
- Handle refresh token lifecycle.

### Organizations

- Manage organization entities and memberships.
- CRUD on organizations and member roles.

### Boards & Permissions

- CRUD for boards and board visibility.
- Manage board collaborators and their roles.
- Query boards by owner, organization, or visibility.

### Sections & Tabs

- Retrieve and reorder sections within boards.
- Manage tabs under sections or boards.
- JSON-based layout metadata stored in `layout_meta`.

### Cards, Tags & Links

- CRUD for cards with support for task/event metadata.
- Attach tags to cards through `card_tags`.
- Manage cross-card relationships via `links`.

### Clustering & Rules

- Manage AI/logic-driven board clusters.
- Store rule definitions as JSON (`rule_def`).
- Update cluster visualization metadata (`visual_meta`).

### Activities

- Record user and system events (card moved, link added, etc.).
- Auto-triggers worker jobs for rule evaluation and clustering.

### Files & Operations

- Associate uploaded files with users, boards, and cards.
- Log client-side or system operations for synchronization.

### Worker Queue

- Persistent background job system (`worker_jobs`).
- Tracks attempts and deadlettered jobs.
- Enum-based status transitions: `queued → running → succeeded/failed`.

### Messaging & Invites

- Internal message system for user communication.
- Invitation flow to friends, boards and organizations.

------

## 8. JSONB and Metadata Handling

All JSON-based columns (`theme`, `rules`, `layout_meta`, `payload`, etc.)
 use `JsonDocument` in models and `GetRawText()` in repositories.

To prevent type mismatches in PostgreSQL 17+,
 `NpgsqlDbType.Jsonb` is recommended for future additions:

```csharp
cmd.Parameters.Add("meta", NpgsqlDbType.Jsonb).Value = e.LayoutMeta.RootElement.GetRawText();
```

------

## 9. Error Handling and Transactions

- Each repository method is expected to throw standard exceptions (`NpgsqlException`, `InvalidOperationException`).
- Multi-operation consistency should be implemented at the **service layer**, using explicit transactions (`BeginTransactionAsync()`).

------

## 10. Database Conventions

| Aspect        | Convention                                                   |
| ------------- | ------------------------------------------------------------ |
| Primary Keys  | `uuid` (generated via `gen_random_uuid()`)                   |
| Foreign Keys  | Always `ON DELETE CASCADE` or `SET NULL` as appropriate.     |
| Timestamps    | `created_at`, `updated_at` (trigger-based auto update).      |
| Enum Columns  | PostgreSQL native enums (no integer mapping).                |
| JSONB Columns | Used for dynamic metadata and payloads.                      |
| Indexing      | Key tables have GIN/GiST indexes where needed (e.g. text search, trigram). |

------

## 11. Extending the Data Layer

To add a new repository:

1. Create a new model class under `Models/<Domain>/`.
2. Create a corresponding repository inheriting `RepositoryBase<T>`.
3. Follow existing parameterization and async patterns.
4. Register the new service that consumes it in DI (`builder.Services.AddScoped<...>()`).

For schema changes:

- Update `001_schema.sql`.
- Add new enum mapping in `Program.cs` if needed.
- Run migrations manually or via script.

------

## 12. Summary

The StickyBoard data layer is designed for:

- **Clarity:** Explicit SQL, clear namespaces.
- **Safety:** Async I/O, token propagation, strong typing.
- **Extensibility:** JSONB support and modular repositories.
- **Maintainability:** Each table and concern isolated by namespace.

**Status:**
 ✅ Schema validated
 ✅ Enums mapped
 ✅ Models synchronized
 ✅ Repositories complete

------

*This document should be kept up to date whenever schema or repository definitions change.*