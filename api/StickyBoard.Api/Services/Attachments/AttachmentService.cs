using StickyBoard.Api.DTOs.Attachments;
using StickyBoard.Api.Models.Attachments;
using StickyBoard.Api.Repositories.Attachments.Contracts;
using StickyBoard.Api.Services.Attachments.Contracts;

namespace StickyBoard.Api.Services.Attachments;

public sealed class AttachmentService : IAttachmentService
{
    private readonly IAttachmentRepository _attachments;

    public AttachmentService(IAttachmentRepository attachments)
    {
        _attachments = attachments;
    }

    public async Task<AttachmentDto> CreateAsync(Guid uploaderId, AttachmentCreateDto dto, CancellationToken ct)
    {
        var e = new Attachment
        {
            WorkspaceId = dto.WorkspaceId,
            BoardId = dto.BoardId,
            CardId = dto.CardId,
            Filename = dto.Filename,
            Mime = dto.Mime,
            ByteSize = dto.ByteSize,
            StoragePath = dto.StoragePath,
            IsPublic = dto.IsPublic,
            Status = dto.Status,
            Meta = dto.Meta,
            UploadedBy = uploaderId
        };

        var id = await _attachments.CreateAsync(e, ct);
        var created = await _attachments.GetByIdAsync(id, ct);

        return Map(created!);
    }

    public async Task<bool> UpdateAsync(Guid id, AttachmentUpdateDto dto, CancellationToken ct)
    {
        var existing = await _attachments.GetByIdAsync(id, ct);
        if (existing is null)
            return false;

        if (dto.Filename is not null) existing.Filename = dto.Filename;
        if (dto.Mime is not null) existing.Mime = dto.Mime;
        if (dto.ByteSize.HasValue) existing.ByteSize = dto.ByteSize;
        if (dto.IsPublic.HasValue) existing.IsPublic = dto.IsPublic.Value;
        if (dto.Status is not null) existing.Status = dto.Status;
        if (dto.Meta is not null) existing.Meta = dto.Meta;

        existing.Version = dto.Version;

        return await _attachments.UpdateAsync(existing, ct);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        => _attachments.DeleteAsync(id, ct);

    public async Task<IEnumerable<AttachmentDto>> GetForCardAsync(Guid cardId, CancellationToken ct)
    {
        var list = await _attachments.GetForCardAsync(cardId, ct);
        return list.Select(Map);
    }

    public async Task<IEnumerable<AttachmentDto>> GetForBoardAsync(Guid boardId, CancellationToken ct)
    {
        var list = await _attachments.GetForBoardAsync(boardId, ct);
        return list.Select(Map);
    }

    public async Task<IEnumerable<AttachmentDto>> GetForWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        var list = await _attachments.GetForWorkspaceAsync(workspaceId, ct);
        return list.Select(Map);
    }

    public async Task<AttachmentDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var e = await _attachments.GetByIdAsync(id, ct);
        return e is null ? null : Map(e);
    }

    private static AttachmentDto Map(Attachment a) => new()
    {
        Id = a.Id,
        WorkspaceId = a.WorkspaceId,
        BoardId = a.BoardId,
        CardId = a.CardId,
        Filename = a.Filename,
        Mime = a.Mime,
        ByteSize = a.ByteSize,
        StoragePath = a.StoragePath,
        IsPublic = a.IsPublic,
        Status = a.Status,
        Meta = a.Meta,
        UploadedBy = a.UploadedBy,
        Version = a.Version,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
