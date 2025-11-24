namespace StickyBoard.Core.Models.Base;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}