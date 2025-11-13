using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.DTOs.SocialAndMessaging;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public sealed class InviteRepository : RepositoryBase<Invite>, IInviteRepository
{
    public InviteRepository(NpgsqlDataSource db) : base(db) { }

    // =====================================================================
    // Mapping
    // =====================================================================
    protected override Invite Map(NpgsqlDataReader r)
        => MappingHelper.MapEntity<Invite>(r);

    // =====================================================================
    // CREATE via DB helper (invite_create)
    // =====================================================================
    public async Task<Guid> CreateViaDbFunctionAsync(
        Guid senderId,
        string email,
        InviteScope scopeType,
        Guid? workspaceId,
        Guid? boardId,
        Guid? contactId,
        WorkspaceRole? targetRole,
        WorkspaceRole? boardRole,
        string tokenHash,
        TimeSpan expiresIn,
        string? note,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT invite_create(
                @sender,
                @em,
                @scope,
                @ws,
                @board,
                @contact,
                @trole,
                @brole,
                @token_hash,
                @expires_in,
                @note
            ) AS id;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("sender", senderId);
        cmd.Parameters.AddWithValue("em", email);
        cmd.Parameters.AddWithValue("scope", scopeType.ToString());
        cmd.Parameters.AddWithValue("ws", (object?)workspaceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("board", (object?)boardId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("contact", (object?)contactId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("trole", (object?)targetRole ?? DBNull.Value);
        cmd.Parameters.AddWithValue("brole", (object?)boardRole ?? DBNull.Value);
        cmd.Parameters.AddWithValue("token_hash", tokenHash);
        cmd.Parameters.AddWithValue("expires_in", expiresIn);
        cmd.Parameters.AddWithValue("note", (object?)note ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync(ct);
        return (Guid)result!;
    }

    // =====================================================================
    // ACCEPT via DB helper (invite_accept)
    // =====================================================================
    public async Task<InviteAcceptResponseDto?> AcceptViaDbFunctionAsync(
        string tokenHash,
        Guid acceptingUserId,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                invite_id,
                scope_type,
                workspace_id,
                board_id,
                contact_id,
                target_role,
                board_role
            FROM invite_accept(@hash, @uid);
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("hash", tokenHash);
        cmd.Parameters.AddWithValue("uid", acceptingUserId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;

        return new InviteAcceptResponseDto();
    }

    // =====================================================================
    // REVOKE via DB helper
    // =====================================================================
    public async Task<bool> RevokeViaDbFunctionAsync(
        string tokenHash,
        CancellationToken ct)
    {
        const string sql = @"SELECT invite_revoke(@hash);";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("hash", tokenHash);
        await cmd.ExecuteNonQueryAsync(ct);

        return true;
    }

    // =====================================================================
    // Get Invites by Sender
    // =====================================================================
    public async Task<IEnumerable<Invite>> GetBySenderAsync(
        Guid senderId,
        CancellationToken ct)
    {
        var sql = $@"
            SELECT *
            FROM {Table}
            WHERE sender_id = @sender
            ORDER BY created_at DESC;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("sender", senderId);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    // =====================================================================
    // Get Invites by Email
    // =====================================================================
    public async Task<IEnumerable<Invite>> GetByEmailAsync(
        string email,
        CancellationToken ct)
    {
        var sql = $@"
            SELECT *
            FROM {Table}
            WHERE LOWER(email) = LOWER(@em)
            ORDER BY created_at DESC;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);
        cmd.Parameters.AddWithValue("em", email);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    // =====================================================================
    // Get Invite by Token Hash
    // =====================================================================
    public async Task<Invite?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken ct)
    {
        var sql = $@"
            SELECT *
            FROM {Table}
            WHERE token_hash = @hash
            LIMIT 1;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("hash", tokenHash);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    // =====================================================================
    // Validation helpers
    // =====================================================================
    public async Task<bool> ValidateWorkspaceExistsAsync(
        Guid workspaceId,
        CancellationToken ct)
    {
        const string sql = @"SELECT 1 FROM workspaces WHERE id = @id";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", workspaceId);
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<bool> ValidateBoardExistsAsync(
        Guid boardId,
        CancellationToken ct)
    {
        const string sql = @"SELECT 1 FROM boards WHERE id = @id";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("id", boardId);
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    // =====================================================================
    // RAW CREATE/UPDATE (NOT SUPPORTED)
    // =====================================================================
    public override Task<Guid> CreateAsync(Invite e, CancellationToken ct)
        => throw new NotSupportedException(
            "Use CreateViaDbFunctionAsync for invite creation.");

    public override Task<bool> UpdateAsync(Invite e, CancellationToken ct)
        => throw new NotSupportedException(
            "Use database helper functions for modifying invite state.");
}
