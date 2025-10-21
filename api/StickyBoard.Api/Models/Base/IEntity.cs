namespace StickyBoard.Api.Models.Base;

public interface IEntity
{
    Guid Id { get; set; }
    DateTime CreatedAt { get; set; }
}