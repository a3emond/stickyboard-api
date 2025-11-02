using System.Text.Json;
using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards;
using StickyBoard.Api.Repositories.Organizations;

namespace StickyBoard.Api.Services;

public sealed class BoardFolderService
{
    private readonly BoardFolderRepository _folders;
    private readonly OrganizationMemberRepository _orgMembers;

    public BoardFolderService(
        BoardFolderRepository folders,
        OrganizationMemberRepository orgMembers)
    {
        _folders = folders;
        _orgMembers = orgMembers;
    }

    public async Task<Guid> CreateAsync(Guid actorId, BoardFolderCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("Folder name is required.");

        // Private folder if no org
        if (dto.OrgId is null)
        {
            var folder = new BoardFolder
            {
                UserId = actorId,
                Name = dto.Name.Trim(),
                Icon = dto.Icon,
                Color = dto.Color,
                Meta = JsonSerializer.SerializeToDocument(dto.Meta ?? new { })
            };
            return await _folders.CreateAsync(folder, ct);
        }

        // Org folder: ensure actor is owner/admin
        var members = await _orgMembers.GetByOrganizationAsync(dto.OrgId.Value, ct);
        var self = members.FirstOrDefault(m => m.UserId == actorId)
            ?? throw new ForbiddenException("Only organization members can create org folders.");

        if (self.Role is not OrgRole.owner and not OrgRole.admin)
            throw new ForbiddenException("Only org owner/admin can create org folders.");

        var orgFolder = new BoardFolder
        {
            OrgId = dto.OrgId,
            Name = dto.Name.Trim(),
            Icon = dto.Icon,
            Color = dto.Color,
            Meta = JsonSerializer.SerializeToDocument(dto.Meta ?? new { })
        };

        return await _folders.CreateAsync(orgFolder, ct);
    }

    public async Task<bool> UpdateAsync(Guid actorId, Guid folderId, BoardFolderUpdateDto dto, CancellationToken ct)
    {
        var folder = await _folders.GetByIdAsync(folderId, ct)
                    ?? throw new NotFoundException("Folder not found.");

        // Check owner access
        if (folder.UserId != null && folder.UserId != actorId)
            throw new ForbiddenException("You do not own this folder.");

        if (folder.OrgId != null)
        {
            var members = await _orgMembers.GetByOrganizationAsync(folder.OrgId.Value, ct);
            var self = members.FirstOrDefault(m => m.UserId == actorId)
                ?? throw new ForbiddenException("Not part of organization.");

            if (self.Role is not OrgRole.owner and not OrgRole.admin)
                throw new ForbiddenException("Only org owner/admin can edit this folder.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
            folder.Name = dto.Name.Trim();
        if (dto.Icon != null)
            folder.Icon = dto.Icon;
        if (dto.Color != null)
            folder.Color = dto.Color;
        if (dto.Meta is not null)
            folder.Meta = JsonSerializer.SerializeToDocument(dto.Meta);

        return await _folders.UpdateAsync(folder, ct);
    }

    public async Task<bool> DeleteAsync(Guid actorId, Guid folderId, CancellationToken ct)
    {
        var folder = await _folders.GetByIdAsync(folderId, ct)
                    ?? throw new NotFoundException("Folder not found.");

        if (folder.UserId != null && folder.UserId != actorId)
            throw new ForbiddenException("You do not own this folder.");

        if (folder.OrgId != null)
        {
            var members = await _orgMembers.GetByOrganizationAsync(folder.OrgId.Value, ct);
            var self = members.FirstOrDefault(m => m.UserId == actorId)
                ?? throw new ForbiddenException("Not part of organization.");

            if (self.Role is not OrgRole.owner and not OrgRole.admin)
                throw new ForbiddenException("Only org owner/admin can delete this folder.");
        }

        return await _folders.DeleteAsync(folderId, ct);
    }

    public async Task<IEnumerable<BoardFolderDto>> GetAccessibleAsync(Guid actorId, CancellationToken ct)
    {
        var folders = await _folders.GetAccessibleFoldersAsync(actorId, ct);

        return folders.Select(f => new BoardFolderDto
        {
            Id = f.Id,
            Name = f.Name,
            OrgId = f.OrgId,
            UserId = f.UserId,
            Icon = f.Icon,
            Color = f.Color,
            Meta = f.Meta.Deserialize<object>() ?? new { }
        });
    }
}
