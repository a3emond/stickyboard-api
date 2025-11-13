using StickyBoard.Api.DTOs.SocialAndMessaging;

namespace StickyBoard.Api.Services.SocialAndMessaging.Contracts;

public interface IInviteService
{
    Task<InviteCreateResponseDto> CreateAsync(InviteCreateRequestDto dto, CancellationToken ct);
    Task<InviteAcceptResponseDto?> AcceptAsync(InviteAcceptRequestDto dto, CancellationToken ct);
    Task<bool> RevokeAsync(string token, CancellationToken ct);

    Task<IEnumerable<InviteDto>> GetBySenderAsync(Guid senderId, CancellationToken ct);
    Task<IEnumerable<InviteDto>> GetByEmailAsync(string email, CancellationToken ct);
    Task<InviteDto?> GetByTokenAsync(string token, CancellationToken ct);
}