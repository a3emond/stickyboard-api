using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Models.UsersAndAuth;
using StickyBoard.Api.Repositories.Base;

namespace StickyBoard.Api.Repositories.UsersAndAuth
{
    public class RefreshTokenRepository : RepositoryBase<RefreshToken>
    {
        public RefreshTokenRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        protected override RefreshToken Map(NpgsqlDataReader reader)
            => MappingHelper.MapEntity<RefreshToken>(reader);

        // ----------------------------------------------------------------------
        // CREATE (token hash is PK, so return Guid.Empty)
        // ----------------------------------------------------------------------
        public override async Task<Guid> CreateAsync(RefreshToken e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO refresh_tokens (token_hash, user_id, expires_at, revoked)
                VALUES (@hash, @uid, @exp, FALSE)", conn);

            cmd.Parameters.AddWithValue("hash", e.TokenHash);
            cmd.Parameters.AddWithValue("uid", e.UserId);
            cmd.Parameters.AddWithValue("exp", e.ExpiresAt);

            await cmd.ExecuteNonQueryAsync(ct);

            // Return Guid.Empty because token_hash is the real key
            return Guid.Empty;
        }

        // ----------------------------------------------------------------------
        // UPDATE (revoke one token)
        // ----------------------------------------------------------------------
        public override async Task<bool> UpdateAsync(RefreshToken e, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE refresh_tokens
                   SET revoked = @rev
                 WHERE token_hash = @hash", conn);

            cmd.Parameters.AddWithValue("rev", e.Revoked);
            cmd.Parameters.AddWithValue("hash", e.TokenHash);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // DELETE (invalidate *all* sessions for a user)
        // Hard delete makes sense for security tokens.
        // ----------------------------------------------------------------------
        public override async Task<bool> DeleteAsync(Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "DELETE FROM refresh_tokens WHERE user_id = @id", conn);

            cmd.Parameters.AddWithValue("id", userId);

            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // GET VALID TOKEN BY HASH
        // Ensures not revoked & not expired
        // ----------------------------------------------------------------------
        public async Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM refresh_tokens
                 WHERE token_hash = @hash
                   AND revoked = FALSE
                   AND expires_at > NOW()
                 LIMIT 1", conn);

            cmd.Parameters.AddWithValue("hash", hash);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        // ----------------------------------------------------------------------
        // REVOKE ONE TOKEN
        // ----------------------------------------------------------------------
        public async Task<bool> RevokeTokenAsync(string hash, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                UPDATE refresh_tokens
                   SET revoked = TRUE
                 WHERE token_hash = @hash", conn);

            cmd.Parameters.AddWithValue("hash", hash);
            return await cmd.ExecuteNonQueryAsync(ct) > 0;
        }

        // ----------------------------------------------------------------------
        // REVOKE ALL TOKENS FOR USER (logout everywhere)
        // ----------------------------------------------------------------------
        public async Task RevokeAllAsync(Guid userId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "UPDATE refresh_tokens SET revoked = TRUE WHERE user_id = @uid", conn);

            cmd.Parameters.AddWithValue("uid", userId);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        // ----------------------------------------------------------------------
        // LIST TOKENS FOR USER
        // ----------------------------------------------------------------------
        public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var list = new List<RefreshToken>();

            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(@"
                SELECT * FROM refresh_tokens
                 WHERE user_id = @uid
                 ORDER BY created_at DESC", conn);

            cmd.Parameters.AddWithValue("uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
                list.Add(Map(reader));

            return list;
        }
    }
}
