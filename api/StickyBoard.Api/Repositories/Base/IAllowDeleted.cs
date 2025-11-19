namespace StickyBoard.Api.Repositories.Base
{
    /*
        ===================================================================
        IAllowDeleted — Controls visibility of SOFT-DELETED rows
        ===================================================================

        This interface applies ONLY to entities that implement ISoftDeletable
        (i.e., tables with a `deleted_at` column, soft delete model).

        • When a repository implements IAllowDeleted and IncludeDeleted = true:
              SELECT queries will return ALL rows, including those where
              deleted_at IS NOT NULL.

        • When a repository does NOT implement IAllowDeleted:
              RepositoryBase automatically injects:
                  WHERE deleted_at IS NULL
              to hide soft-deleted rows.

        • IMPORTANT:
              IAllowDeleted does NOT relate to hard deletes.
              It has zero effect on tables without a deleted_at column.

        In short:
              IAllowDeleted = "Show soft-deleted rows"
              NOT "Allow hard delete" or anything else.

        Typical use cases:
        ------------------
        - Admin/maintenance operations
        - Sync endpoints that must detect tombstones
        - Debug tools that must see everything
        - Background workers scanning all rows

        If an entity does NOT support soft delete,
        this interface has no effect at all.
        ===================================================================
    */
    public interface IAllowDeleted
    {
        bool IncludeDeleted { get; }
    }
}