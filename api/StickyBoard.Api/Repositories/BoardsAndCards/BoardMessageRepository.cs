using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.BoardsAndCards
{
    public class BoardMessageRepository : RepositoryBase<BoardMessage>
    {
        public BoardMessageRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override BoardMessage Map(NpgsqlDataReader r)
            => MappingHelper.MapEntity<BoardMessage>(r);

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public override async Task<Guid> CreateAsync(BoardMessage e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO board_messages (board_id, user_id, content)
                VALUES (@board, @user, @content)
                RETURNING id;", conn);

            cmd.Parameters.AddWithValue("board", e.BoardId);
            cmd.Parameters.AddWithValue("user", e.UserId);
            cmd.Parameters.AddWithValue("content", e.Content);

            return (Guid)await cmd.ExecuteScalarAsync(ct);
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------
        public override async Task<bool> UpdateAsync(BoardMessage e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE board_messages
                   SET content=@content,
                       updated_at=now()
                 WHERE id=@id;", conn);

            cmd.Parameters.AddWithValue("id", e.Id);
            cmd.Parameters.AddWithValue("content", e.Content);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // SOFT DELETE
        // ------------------------------------------------------------
        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE board_messages
                   SET deleted_at = now(),
                       updated_at = now()
                 WHERE id = @id;", conn);

            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ------------------------------------------------------------
        // GET THREAD
        // ------------------------------------------------------------
        public async Task<IEnumerable<BoardMessage>> GetByBoardAsync(Guid boardId, CancellationToken ct)
        {
            var list = new List<BoardMessage>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM board_messages
                WHERE board_id = @board AND deleted_at IS NULL
                ORDER BY created_at ASC;", conn);

            cmd.Parameters.AddWithValue("board", boardId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
