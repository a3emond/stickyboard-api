namespace StickyBoard.Core.DTOs.Common;

public interface IInviteAware
{
    string? InviteToken { get; set; }
}