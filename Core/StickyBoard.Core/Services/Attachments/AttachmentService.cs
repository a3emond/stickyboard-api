using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Models.Attachments;
using StickyBoard.Core.Repositories.Attachments.Contracts;
using StickyBoard.Core.Services.Attachments.Contracts;
using StickyBoard.Core.Services.Automation.Workers;
using Microsoft.Extensions.Configuration;
using StickyBoard.Core.Models;

namespace StickyBoard.Core.Services.Attachments;

public sealed class AttachmentService : IAttachmentService
{
    private readonly IAttachmentRepository _attachments;
    private readonly IFileTokenRepository _tokens;
    private readonly WorkerQueueService _queue;
    private readonly IConfiguration _config;

    public AttachmentService(
        IAttachmentRepository attachments,
        IFileTokenRepository tokens,
        WorkerQueueService queue,
        IConfiguration config)
    {
        _attachments = attachments;
        _tokens = tokens;
        _queue = queue;
        _config = config;
    }

    // ------------------------------------------------------------
    // CREATE (metadata only)
    // ------------------------------------------------------------
    public async Task<AttachmentDto> CreateAsync(Guid uploaderId, AttachmentCreateDto dto, CancellationToken ct)
    {
        var e = new Attachment
        {
            WorkspaceId  = dto.WorkspaceId,
            BoardId      = dto.BoardId,
            CardId       = dto.CardId,
            Filename     = dto.Filename,
            Mime         = dto.Mime,
            ByteSize     = dto.ByteSize,
            StoragePath  = dto.StoragePath,
            IsPublic     = dto.IsPublic,
            Status       = dto.Status,
            Meta         = dto.Meta,
            UploadedBy   = uploaderId
        };

        var id = await _attachments.CreateAsync(e, ct);
        var created = await _attachments.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Attachment creation failed");

        // Push variant worker
        await _queue.EnqueueAsync(
            WorkerJobKind.AssetVariant,
            JsonDocument.Parse($$"""
            {
                "attachmentId": "{{created.Id}}",
                "storagePath": "{{created.StoragePath}}",
                "mime": "{{created.Mime}}"
            }
            """),
            null,
            ct
        );

        return Map(created);
    }

    // ------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------
    public async Task<bool> UpdateAsync(Guid id, AttachmentUpdateDto dto, CancellationToken ct)
    {
        var existing = await _attachments.GetByIdAsync(id, ct);
        if (existing is null) return false;

        if (dto.Filename is not null) existing.Filename = dto.Filename;
        if (dto.Mime is not null)     existing.Mime = dto.Mime;
        if (dto.ByteSize.HasValue)    existing.ByteSize = dto.ByteSize;
        if (dto.IsPublic.HasValue)    existing.IsPublic = dto.IsPublic.Value;
        if (dto.Status is not null)   existing.Status = (AttachmentStatus)dto.Status;
        if (dto.Meta is not null)     existing.Meta = dto.Meta;

        existing.Version = dto.Version;

        return await _attachments.UpdateAsync(existing, ct);
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        => _attachments.DeleteAsync(id, ct);

    // ------------------------------------------------------------
    // GET
    // ------------------------------------------------------------
    public async Task<IEnumerable<AttachmentDto>> GetForCardAsync(Guid cardId, CancellationToken ct)
        => (await _attachments.GetForCardAsync(cardId, ct)).Select(Map);

    public async Task<IEnumerable<AttachmentDto>> GetForBoardAsync(Guid boardId, CancellationToken ct)
        => (await _attachments.GetForBoardAsync(boardId, ct)).Select(Map);

    public async Task<IEnumerable<AttachmentDto>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct)
        => (await _attachments.GetForWorkspaceAsync(workspaceId, ct)).Select(Map);

    public async Task<AttachmentDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var e = await _attachments.GetByIdAsync(id, ct);
        return e is null ? null : Map(e);
    }

    // ------------------------------------------------------------
    // CDN TOKEN (UPLOAD)
    // ------------------------------------------------------------
    public async Task<string> GenerateUploadUrlAsync(Guid attachmentId, CancellationToken ct)
    {
        var attachment = await _attachments.GetByIdAsync(attachmentId, ct)
            ?? throw new InvalidOperationException("Attachment not found");

        var secret = RandomBytes(32);
        var expires = DateTime.UtcNow.AddMinutes(10);

        var token = new FileToken
        {
            AttachmentId = attachment.Id,
            Variant      = null,
            Secret       = secret,
            Audience     = "upload",
            ExpiresAt    = expires,
            CreatedBy    = attachment.UploadedBy,
            Revoked      = false
        };

        var tokenId = await _tokens.CreateAsync(token, ct);

        var sig = ComputeSignature(
            attachment.StoragePath,
            tokenId,
            expires,
            secret
        );

        return $"{_config["Cdn:BaseUrl"]}/{_config["Cdn:Tenant"]}/protected" +
               $"?path={Uri.EscapeDataString(attachment.StoragePath)}" +
               $"&tid={tokenId}" +
               $"&exp={new DateTimeOffset(expires).ToUnixTimeSeconds()}" +
               $"&sig={sig}";
    }

    // ------------------------------------------------------------
    // CDN TOKEN (DOWNLOAD)
    // ------------------------------------------------------------
    public async Task<string> GenerateDownloadUrlAsync(Guid attachmentId, string? variant, CancellationToken ct)
    {
        var attachment = await _attachments.GetByIdAsync(attachmentId, ct)
            ?? throw new InvalidOperationException("Attachment not found");

        var path = variant is null
            ? attachment.StoragePath
            : attachment.StoragePath.Replace("/original/", $"/{variant}/");

        var secret = RandomBytes(32);
        var expires = DateTime.UtcNow.AddMinutes(60);

        var token = new FileToken
        {
            AttachmentId = attachment.Id,
            Variant      = variant,
            Secret       = secret,
            Audience     = "download",
            ExpiresAt    = expires,
            CreatedBy    = attachment.UploadedBy,
            Revoked      = false
        };

        var tokenId = await _tokens.CreateAsync(token, ct);

        var sig = ComputeSignature(
            path,
            tokenId,
            expires,
            secret
        );

        return $"{_config["Cdn:BaseUrl"]}/{_config["Cdn:Tenant"]}/protected" +
               $"?path={Uri.EscapeDataString(path)}" +
               $"&tid={tokenId}" +
               $"&exp={new DateTimeOffset(expires).ToUnixTimeSeconds()}" +
               $"&sig={sig}";
    }

    // ------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------
    private static byte[] RandomBytes(int len)
    {
        var b = new byte[len];
        RandomNumberGenerator.Fill(b);
        return b;
    }

    private static string ComputeSignature(string path, Guid tid, DateTime exp, byte[] secret)
    {
        var payload = $"path={path}&tid={tid}&exp={new DateTimeOffset(exp).ToUnixTimeSeconds()}";

        using var hmac = new HMACSHA256(secret);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
    
    public async Task FailAsync(Guid id, CancellationToken ct)
    {
        var existing = await _attachments.GetByIdAsync(id, ct)
                       ?? throw new InvalidOperationException("Attachment not found");

        existing.Status = AttachmentStatus.Failed;
        await _attachments.UpdateAsync(existing, ct);
    }

    public async Task<AttachmentDto> MarkReadyAsync(Guid id, CancellationToken ct)
    {
        var existing = await _attachments.GetByIdAsync(id, ct)
                       ?? throw new InvalidOperationException("Attachment not found");

        existing.Status = AttachmentStatus.Ready;
        await _attachments.UpdateAsync(existing, ct);

        return Map(existing);
    }


    private static AttachmentDto Map(Attachment a) => new()
    {
        Id           = a.Id,
        WorkspaceId  = a.WorkspaceId,
        BoardId      = a.BoardId,
        CardId       = a.CardId,
        Filename     = a.Filename,
        Mime         = a.Mime,
        ByteSize     = a.ByteSize,
        StoragePath  = a.StoragePath,
        IsPublic     = a.IsPublic,
        Status       = a.Status,
        Meta         = a.Meta,
        UploadedBy   = a.UploadedBy,
        Version      = a.Version,
        CreatedAt    = a.CreatedAt,
        UpdatedAt    = a.UpdatedAt
    };
}
