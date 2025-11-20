using StickyBoard.Api.Common.Exceptions;
using StickyBoard.Api.DTOs.BoardsAndCards;
using StickyBoard.Api.Models;
using StickyBoard.Api.Models.BoardsAndCards;
using StickyBoard.Api.Repositories.BoardsAndCards.Contracts;
using StickyBoard.Api.Services.BoardsAndCards.Contracts;

namespace StickyBoard.Api.Services;

public sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceMemberRepository _memberRepo;

    public WorkspaceService(
        IWorkspaceRepository workspaceRepo,
        IWorkspaceMemberRepository memberRepo)
    {
        _workspaceRepo = workspaceRepo;
        _memberRepo = memberRepo;
    }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public async Task<WorkspaceDto> CreateAsync(Guid creatorId, WorkspaceCreateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("Workspace name is required.");

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            CreatedBy = creatorId
        };

        await _workspaceRepo.CreateAsync(workspace, ct);

        var fresh = await _workspaceRepo.GetByIdAsync(workspace.Id, ct)
                    ?? throw new ConflictException("Workspace creation failed.");


        return Map(fresh);
    }

    // ---------------------------------------------------------------------
    // GET FOR USER
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<WorkspaceDto>> GetForUserAsync(Guid userId, CancellationToken ct)
    {
        var list = await _workspaceRepo.GetForUserAsync(userId, ct);

        return list.Select(Map);
    }

    // ---------------------------------------------------------------------
    // RENAME
    // ---------------------------------------------------------------------
    public async Task RenameAsync(Guid workspaceId, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Workspace name is required.");

        var ws = await _workspaceRepo.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException("Workspace not found.");

        ws.Name = name.Trim();

        var ok = await _workspaceRepo.UpdateAsync(ws, ct);

        if (!ok)
            throw new ConflictException("Workspace update conflict.");
    }

    // ---------------------------------------------------------------------
    // ADD MEMBER
    // ---------------------------------------------------------------------
    public async Task AddMemberAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct)
    {
        if (!await _workspaceRepo.ExistsAsync(workspaceId, ct))
            throw new NotFoundException("Workspace not found.");

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role
        };

        var ok = await _memberRepo.AddAsync(member, ct);

        if (!ok)
            throw new ConflictException("User already in workspace.");
    }

    // ---------------------------------------------------------------------
    // REMOVE MEMBER
    // ---------------------------------------------------------------------
    public async Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var ok = await _memberRepo.RemoveAsync(workspaceId, userId, ct);

        if (!ok)
            throw new NotFoundException("Workspace member not found.");
    }

    // ---------------------------------------------------------------------
    // CHANGE ROLE
    // ---------------------------------------------------------------------
    public async Task ChangeRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole role, CancellationToken ct)
    {
        var ok = await _memberRepo.UpdateRoleAsync(workspaceId, userId, role, ct);

        if (!ok)
            throw new NotFoundException("Workspace member not found.");
    }

    // ---------------------------------------------------------------------
    // GET MEMBERS
    // ---------------------------------------------------------------------
    public async Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, CancellationToken ct)
    {
        var members = await _memberRepo.GetByWorkspaceAsync(workspaceId, ct);

        return members.Select(x => new WorkspaceMemberDto
        {
            UserId = x.UserId,
            Role = x.Role,
            JoinedAt = x.JoinedAt
        });
    }
    
    // ---------------------------------------------------------------------
    // GET USER ROLE CONVENIENCE
    // ---------------------------------------------------------------------
    public async Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, Guid userId, CancellationToken ct)
        => await _memberRepo.GetRoleAsync(workspaceId, userId, ct);


    // ---------------------------------------------------------------------
    // DELETE
    // ---------------------------------------------------------------------
    public async Task DeleteAsync(Guid workspaceId, CancellationToken ct)
    {
        var ok = await _workspaceRepo.SoftDeleteCascadeAsync(workspaceId, ct);

        if (!ok)
            throw new NotFoundException("Workspace not found or already deleted.");
    }

    // ---------------------------------------------------------------------
    // MAPPING
    // ---------------------------------------------------------------------
    private static WorkspaceDto Map(Workspace w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        CreatedBy = w.CreatedBy,
        CreatedAt = w.CreatedAt,
        UpdatedAt = w.UpdatedAt
    };
}
