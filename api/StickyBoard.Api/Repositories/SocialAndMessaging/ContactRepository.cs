using Npgsql;
using StickyBoard.Api.Common;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.SocialAndMessaging;
using StickyBoard.Api.Repositories.Base;
using StickyBoard.Api.Repositories.SocialAndMessaging.Contracts;

namespace StickyBoard.Api.Repositories.SocialAndMessaging;

public sealed class ContactRepository 
    : RepositoryBase<UserContact>, IContactRepository
{
    public ContactRepository(NpgsqlDataSource db) : base(db) { }

    protected override UserContact MapRow(NpgsqlDataReader r)
        => MappingHelper.MapEntity<UserContact>(r);

    // ---------------------------------------------------------------------
    // CREATE BLOCKED (must use invite system)
    // ---------------------------------------------------------------------
    public override Task<Guid> CreateAsync(UserContact entity, CancellationToken ct)
        => throw new ForbiddenException("Direct creation of contacts is forbidden. Use the invite workflow.");

    // ---------------------------------------------------------------------
    // UPDATE: only allowed status change (pending → accepted → blocked)
    // ---------------------------------------------------------------------
    public override async Task<bool> UpdateAsync(UserContact entity, CancellationToken ct)
    {
        const string sql = @"
            UPDATE user_contacts
               SET status = @status,
                   accepted_at = CASE 
                       WHEN @status = 'accepted' AND accepted_at IS NULL THEN NOW()
                       ELSE accepted_at
                   END
             WHERE user_id = @u
               AND contact_id = @c;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("status", entity.Status.ToString());
        cmd.Parameters.AddWithValue("u", entity.UserId);
        cmd.Parameters.AddWithValue("c", entity.ContactId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ---------------------------------------------------------------------
    // EXISTS(user, contact)
    // ---------------------------------------------------------------------
    public async Task<bool> ContactExistsAsync(Guid userId, Guid contactId, CancellationToken ct)
    {
        const string sql = @"
            SELECT 1
              FROM user_contacts
             WHERE user_id = @u AND contact_id = @c;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("u", userId);
        cmd.Parameters.AddWithValue("c", contactId);

        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    // ---------------------------------------------------------------------
    // GET LIST BY STATUS
    // ---------------------------------------------------------------------
    public async Task<List<UserContact>> GetContactsByUserIdAndStatusAsync(
        Guid userId, ContactStatus status, CancellationToken ct)
    {
        const string sql = @"
            SELECT *
              FROM user_contacts
             WHERE user_id = @u
               AND status = @status
             ORDER BY updated_at DESC;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("u", userId);
        cmd.Parameters.AddWithValue("status", status.ToString());

        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await MapListAsync(r, ct);
    }

    // ---------------------------------------------------------------------
    // UPDATE STATUS DIRECTLY
    // ---------------------------------------------------------------------
    public async Task UpdateContactStatusAsync(Guid userId, Guid contactId, ContactStatus status, CancellationToken ct)
    {
        const string sql = @"
            UPDATE user_contacts
               SET status = @status,
                   accepted_at = CASE 
                       WHEN @status = 'accepted' AND accepted_at IS NULL THEN NOW()
                       ELSE accepted_at
                   END
             WHERE user_id = @u AND contact_id = @c;
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("status", status.ToString());
        cmd.Parameters.AddWithValue("u", userId);
        cmd.Parameters.AddWithValue("c", contactId);

        if (await cmd.ExecuteNonQueryAsync(ct) == 0)
            throw new NotFoundException("Contact relation not found.");
    }

    // ---------------------------------------------------------------------
    // RECIPROCAL DELETE (the ONLY valid delete)
    // ---------------------------------------------------------------------
    public async Task<bool> DeleteReciprocatedContactAsync(Guid userId, Guid contactId, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM user_contacts
             WHERE (user_id = @u AND contact_id = @c)
                OR (user_id = @c AND contact_id = @u);
        ";

        await using var c = await Conn(ct);
        await using var cmd = new NpgsqlCommand(sql, c);

        cmd.Parameters.AddWithValue("u", userId);
        cmd.Parameters.AddWithValue("c", contactId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    // ---------------------------------------------------------------------
    // BLOCK BASE DELETE (force reciprocal only)
    // ---------------------------------------------------------------------
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        => throw new ForbiddenException("Direct deletion is forbidden. Use DeleteReciprocatedContactAsync().");
}
