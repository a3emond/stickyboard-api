using System.Security.Cryptography;
using System.Text;
using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Attachments;
using StickyBoard.Core.Repositories.Attachments.Contracts;
using StickyBoard.Core.Services.Automation.Workers;

namespace StickyBoard.Core.Services.Attachments;

public sealed class AttachmentManager : IAttachmentManager
{
    private readonly IAttachmentRepository _attachments;
    private readonly IAttachmentVariantRepository _variants;
    private readonly IFileTokenRepository _tokens;
    private readonly WorkerQueueService _queue;

    public AttachmentManager(
        IAttachmentRepository attachments,
        IAttachmentVariantRepository variants,
        IFileTokenRepository tokens,
        WorkerQueueService queue)
    {
        _attachments = attachments;
        _variants = variants;
        _tokens = tokens;
        _queue = queue;
    }

    // -------------------- CLIENT FLOW --------------------

    public async Task<AttachmentInitResult> InitUploadAsync(Guid userId, AttachmentInitRequest request, CancellationToken ct)
    {
        var attachmentId = Guid.NewGuid();

        var storagePath = BuildPath(request.BoardId, attachmentId, request.Filename, null);

        var entity = new Attachment
        {
            WorkspaceId = request.WorkspaceId,
            BoardId     = request.BoardId,
            CardId      = request.CardId,
            Filename    = request.Filename,
            Mime        = request.Mime,
            ByteSize    = request.ByteSize,
            StoragePath = storagePath,
            IsPublic    = request.IsPublic,
            Status      = AttachmentStatus.Uploading,
            UploadedBy  = userId
        };

        var newId = await _attachments.CreateAsync(entity, ct);

        var uploadUrl = await GenerateSignedUrlAsync(newId, storagePath, "upload", CdnConstants.UploadExpiryMinutes, ct);

        return new AttachmentInitResult
        {
            AttachmentId = newId,
            StoragePath  = storagePath,
            UploadUrl    = uploadUrl
        };
    }

    public async Task MarkUploadFailedAsync(Guid attachmentId, CancellationToken ct)
    {
        var a = await _attachments.GetByIdAsync(attachmentId, ct)
                ?? throw new InvalidOperationException("Attachment not found");

        a.Status = AttachmentStatus.Failed;
        await _attachments.UpdateAsync(a, ct);
    }

    public async Task MarkUploadCompleteAsync(Guid attachmentId, CancellationToken ct)
    {
        var a = await _attachments.GetByIdAsync(attachmentId, ct)
                ?? throw new InvalidOperationException("Attachment not found");

        a.Status = AttachmentStatus.Ready;
        await _attachments.UpdateAsync(a, ct);

        await _queue.EnqueueAsync(
            WorkerJobKind.AssetVariant,
            System.Text.Json.JsonDocument.Parse($$"""
            {
                "attachmentId": "{{a.Id}}",
                "storagePath": "{{a.StoragePath}}",
                "mime": "{{a.Mime}}"
            }
            """),
            null,
            ct
        );
    }

    public async Task<AttachmentDownloadResult> GetDownloadUrlAsync(Guid attachmentId, string? variant, CancellationToken ct)
    {
        var a = await _attachments.GetByIdAsync(attachmentId, ct)
                ?? throw new InvalidOperationException("Attachment not found");

        if (a.Status != AttachmentStatus.Ready)
            throw new InvalidOperationException("Attachment not ready");

        var path = BuildPath(a.BoardId!.Value, attachmentId, a.Filename, variant);

        var url = await GenerateSignedUrlAsync(attachmentId, path, "download", CdnConstants.DownloadExpiryMinutes, ct);

        return new AttachmentDownloadResult
        {
            AttachmentId = a.Id,
            Variant = variant ?? "original",
            DownloadUrl = url
        };
    }

    public async Task<IReadOnlyList<AttachmentMetaDto>> GetForCardAsync(Guid cardId, CancellationToken ct)
    {
        var list = await _attachments.GetForCardAsync(cardId, ct);
        return await ToMeta(list, ct);
    }

    public async Task<IReadOnlyList<AttachmentMetaDto>> GetForBoardAsync(Guid boardId, CancellationToken ct)
    {
        var list = await _attachments.GetForBoardAsync(boardId, ct);
        return await ToMeta(list, ct);
    }

    public async Task<IReadOnlyList<AttachmentMetaDto>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        var list = await _attachments.GetForWorkspaceAsync(workspaceId, ct);
        return await ToMeta(list, ct);
    }

    // -------------------- WORKER FLOW --------------------

    public async Task<string> GetOriginalForProcessingAsync(Guid attachmentId, CancellationToken ct)
    {
        var a = await _attachments.GetByIdAsync(attachmentId, ct)
                ?? throw new InvalidOperationException("Attachment not found");

        return await GenerateSignedUrlAsync(attachmentId, a.StoragePath, "download", 60, ct);
    }

    public async Task<string> GetVariantUploadUrlAsync(Guid attachmentId, AttachmentVariantType variant, CancellationToken ct)
    {
        var a = await _attachments.GetByIdAsync(attachmentId, ct)
                ?? throw new InvalidOperationException("Attachment not found");

        var path = BuildPath(a.BoardId!.Value, attachmentId, a.Filename, variant.ToString());

        return await GenerateSignedUrlAsync(attachmentId, path, "upload", 30, ct);
    }

    public async Task RegisterVariantAsync(VariantRegisterRequest r, CancellationToken ct)
    {
        var v = new AttachmentVariant
        {
            ParentId   = r.AttachmentId,
            Variant    = r.Variant,
            StoragePath= r.StoragePath,
            Mime       = r.Mime,
            ByteSize   = r.ByteSize,
            Width      = r.Width,
            Height     = r.Height,
            DurationMs = r.DurationMs,
            Status     = AttachmentStatus.Ready,
            ChecksumSha256 = r.ChecksumSha256
        };

        await _variants.CreateAsync(v, ct);
    }

    // -------------------- CLEANUP --------------------

    public Task<bool> DeleteAsync(Guid attachmentId, CancellationToken ct)
        => _attachments.DeleteAsync(attachmentId, ct);

    public Task RevokeAllTokensAsync(Guid attachmentId, CancellationToken ct)
        => _tokens.RevokeAllForAttachmentAsync(attachmentId, ct);

    // -------------------- INTERNAL --------------------

    private static string BuildPath(Guid boardId, Guid attachmentId, string filename, string? variant)
    {
        variant ??= "original";
        return $"boards/{boardId}/att/{attachmentId}/{variant}/{filename}";
    }

    private async Task<string> GenerateSignedUrlAsync(
        Guid attachmentId,
        string path,
        string audience,
        int expiryMinutes,
        CancellationToken ct)
    {
        var secret = RandomBytes(32);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new FileToken
        {
            AttachmentId = attachmentId,
            Variant = null,
            Secret = secret,
            Audience = audience,
            ExpiresAt = expires,
            Revoked = false
        };

        var tokenId = await _tokens.CreateAsync(token, ct);

        var sig = ComputeSignature(path, tokenId, expires, secret);

        return $"{CdnConstants.BaseUrl}/{CdnConstants.Tenant}/{CdnConstants.ProtectedEndpoint}" +
               $"?path={Uri.EscapeDataString(path)}" +
               $"&tid={tokenId}" +
               $"&exp={new DateTimeOffset(expires).ToUnixTimeSeconds()}" +
               $"&sig={sig}";
    }

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

    private async Task<IReadOnlyList<AttachmentMetaDto>> ToMeta(IEnumerable<Attachment> attachments, CancellationToken ct)
    {
        var result = new List<AttachmentMetaDto>();

        foreach (var a in attachments)
        {
            var variants = await _variants.GetForParentAsync(a.Id, ct);

            result.Add(new AttachmentMetaDto
            {
                Id = a.Id,
                Filename = a.Filename,
                Mime = a.Mime ?? "",
                ByteSize = a.ByteSize,
                Status = a.Status,
                Variants = variants.Select(v => new VariantMetaDto
                {
                    Variant = v.Variant.ToString(),
                    ByteSize = v.ByteSize,
                    Width = v.Width,
                    Height = v.Height,
                    Status = v.Status,
                    Version = v.Version
                }).ToList()
            });
        }

        return result;
    }
}
