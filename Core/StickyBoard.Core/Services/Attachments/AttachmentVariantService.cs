using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Attachments;
using StickyBoard.Core.Repositories.Attachments.Contracts;
using StickyBoard.Core.Services.Attachments.Contracts;

namespace StickyBoard.Core.Services.Attachments;

public sealed class AttachmentVariantService : IAttachmentVariantService
{
    private readonly IAttachmentVariantRepository _variants;

    public AttachmentVariantService(IAttachmentVariantRepository variants)
    {
        _variants = variants;
    }

    public async Task<AttachmentVariantDto> CreateAsync(AttachmentVariantCreateDto dto, CancellationToken ct)
    {
        var e = new AttachmentVariant
        {
            ParentId = dto.ParentId,
            Variant = dto.Variant,
            Mime = dto.Mime,
            ByteSize = dto.ByteSize,
            Width = dto.Width,
            Height = dto.Height,
            DurationMs = dto.DurationMs,
            StoragePath = dto.StoragePath,
            Status = dto.Status,
            ChecksumSha256 = dto.ChecksumSha256
        };

        var id = await _variants.CreateAsync(e, ct);
        var created = await _variants.GetByIdAsync(id, ct);

        return Map(created!);
    }

    public async Task<bool> UpdateAsync(Guid id, AttachmentVariantUpdateDto dto, CancellationToken ct)
    {
        var existing = await _variants.GetByIdAsync(id, ct);
        if (existing is null)
            return false;

        if (dto.Mime is not null) existing.Mime = dto.Mime;
        if (dto.ByteSize.HasValue) existing.ByteSize = dto.ByteSize;
        if (dto.Width.HasValue) existing.Width = dto.Width;
        if (dto.Height.HasValue) existing.Height = dto.Height;
        if (dto.DurationMs.HasValue) existing.DurationMs = dto.DurationMs;
        if (dto.StoragePath is not null) existing.StoragePath = dto.StoragePath;
        if (dto.Status is not null) existing.Status = (AttachmentStatus)dto.Status;
        if (dto.ChecksumSha256 is not null) existing.ChecksumSha256 = dto.ChecksumSha256;

        return await _variants.UpdateAsync(existing, ct);
    }

    public async Task<IEnumerable<AttachmentVariantDto>> GetForParentAsync(Guid parentId, CancellationToken ct)
    {
        var list = await _variants.GetForParentAsync(parentId, ct);
        return list.Select(Map);
    }

    public async Task<AttachmentVariantDto?> GetAsync(Guid parentId, string variant, CancellationToken ct)
    {
        var list = await _variants.GetForParentAsync(parentId, ct);

        if (!Enum.TryParse<AttachmentVariantType>(variant, true, out var parsedVariant))
            return null;

        var v = list.FirstOrDefault(x => x.Variant == parsedVariant);

        return v is null ? null : Map(v);
    }


    private static AttachmentVariantDto Map(AttachmentVariant v)
    {
        return new AttachmentVariantDto
        {
            Id = v.Id,
            ParentId = v.ParentId,
            Variant = v.Variant,
            Mime = v.Mime,
            ByteSize = v.ByteSize,
            Width = v.Width,
            Height = v.Height,
            DurationMs = v.DurationMs,
            StoragePath = v.StoragePath,
            Status = v.Status,
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt
        };
    }
}