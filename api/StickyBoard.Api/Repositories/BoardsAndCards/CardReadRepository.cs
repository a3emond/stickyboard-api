using Npgsql;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Repositories.BoardsAndCards;

public sealed class CardReadRepository : ICardReadRepository
{
    private readonly NpgsqlDataSource _db;

    public CardReadRepository(NpgsqlDataSource db) => _db = db;

    private ValueTask<NpgsqlConnection> Conn(CancellationToken ct)
        => _db.OpenConnectionAsync(ct);

    // ---------------------------------------------------------------------
    // UPSERT READ
    // ---------------------------------------------------------------------
    public async Task UpsertAsync(Guid cardId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO card_reads (card_id, user_id, last_seen_at)
            VALUES (@card_id, @user_id, NOW())
            ON CONFLICT (card_id, user_id)
            DO UPDATE SET last_seen_at = NOW()
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("card_id", cardId);
        cmd.Parameters.AddWithValue("user_id", userId);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ---------------------------------------------------------------------
    // GET LAST READ
    // ---------------------------------------------------------------------
    public async Task<CardRead?> GetAsync(Guid cardId, Guid userId, CancellationToken ct)
    {
        const string sql = @"
            SELECT card_id, user_id, last_seen_at
            FROM card_reads
            WHERE card_id = @card_id
              AND user_id = @user_id
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("card_id", cardId);
        cmd.Parameters.AddWithValue("user_id", userId);

        await using var r = await cmd.ExecuteReaderAsync(ct);

        if (!await r.ReadAsync(ct))
            return null;

        return new CardRead
        {
            CardId = r.GetGuid(0),
            UserId = r.GetGuid(1),
            LastSeenAt = r.GetDateTime(2)
        };
    }
}