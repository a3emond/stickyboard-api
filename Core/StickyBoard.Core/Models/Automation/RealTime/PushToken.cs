using System.ComponentModel.DataAnnotations.Schema;

namespace StickyBoard.Core.Models.Automation.RealTime;

[Table("push_tokens")]
public class PushToken
{
    [Column("id")] public long Id { get; set; }

    [Column("user_id")] public Guid UserId { get; set; }

    [Column("provider")] public PushProvider Provider { get; set; }

    [Column("token")] public string Token { get; set; } = null!;

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
}
/*
CREATE TABLE IF NOT EXISTS push_tokens (
  id         bigserial PRIMARY KEY,
  user_id    uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  provider   push_provider NOT NULL,
  token      text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE(provider, token)
);
*/