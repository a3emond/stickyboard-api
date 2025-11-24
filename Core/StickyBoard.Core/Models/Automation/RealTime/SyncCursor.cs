using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Core.Models.Automation.RealTime;

[Table("sync_cursor")]
public class SyncCursor
{
    [Column("user_id")] public Guid UserId { get; set; }

    [Column("scope_type")] public SyncScopeType ScopeType { get; set; }

    [Column("scope_id")] public Guid? ScopeId { get; set; }

    [Column("last_cursor")] public long LastCursor { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
/*
  user_id     uuid NOT NULL,
  scope_type  sync_scope_type NOT NULL CHECK (scope_type IN ('workspace','board','inbox')),
  scope_id    uuid,
  last_cursor bigint NOT NULL DEFAULT 0,
  updated_at  timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY(user_id, scope_type, scope_id)
);
*/