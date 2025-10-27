using Npgsql;
using StickyBoard.Api.Models.Users;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories
{
    public class RefreshTokenRepository : RepositoryBase<RefreshToken>
    {
        public RefreshTokenRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override RefreshToken Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<RefreshToken>(reader);

        public override async Task<Guid> CreateAsync(RefreshToken e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO refresh_tokens (token_hash, user_id, expires_at)
                VALUES (@hash, @uid, @exp)
                RETURNING user_id", conn);

            cmd.Parameters.AddWithValue("hash", e.TokenHash);
            cmd.Parameters.AddWithValue("uid", e.UserId);
            cmd.Parameters.AddWithValue("exp", e.ExpiresAt);

            await cmd.ExecuteScalarAsync(ct);
            return e.UserId; // no Guid PK, returns user id
        }

        public override async Task<bool> UpdateAsync(RefreshToken e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE refresh_tokens
                   SET revoked=@rev
                 WHERE token_hash=@hash", conn);

            cmd.Parameters.AddWithValue("rev", e.Revoked);
            cmd.Parameters.AddWithValue("hash", e.TokenHash);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("DELETE FROM refresh_tokens WHERE user_id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM refresh_tokens
                 WHERE token_hash=@hash AND revoked=FALSE
                 LIMIT 1", conn);

            cmd.Parameters.AddWithValue("hash", hash);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        public async Task RevokeAllAsync(Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE refresh_tokens SET revoked=TRUE WHERE user_id=@uid", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        
        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<RefreshToken>();
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT * FROM refresh_tokens WHERE user_id=@u ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("u", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(MappingHelper.MapEntity<RefreshToken>(reader));

            return list;
        }

    }
}
