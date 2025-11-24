using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Core.Models.Automation.RealTime;

[Table("ws_sessions")]
public class WsSession
{
    [Column("id")] public long Id { get; set; }

    [Column("user_id")] public Guid UserId { get; set; }

    [Column("node_id")] public string NodeId { get; set; } = null!;

    [Column("connected_at")] public DateTime ConnectedAt { get; set; }

    [Column("last_seen_at")] public DateTime LastSeenAt { get; set; }
}
/*
CREATE TABLE IF NOT EXISTS ws_sessions (
  id           bigserial PRIMARY KEY,
  user_id      uuid NOT NULL,
  node_id      text NOT NULL,
  connected_at timestamptz NOT NULL DEFAULT now(),
  last_seen_at timestamptz NOT NULL DEFAULT now()
);
*/