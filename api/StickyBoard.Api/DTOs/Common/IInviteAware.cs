namespace StickyBoard.Api.DTOs.Common;

public interface IInviteAware
{
    string? InviteToken { get; set; }
}