namespace StickyBoard.Api.Models.Base
{
    public interface ISoftDeletable
    {
        DateTime? DeletedAt { get; set; }
    }
}