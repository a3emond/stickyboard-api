namespace StickyBoard.Api.Models.Base;

public interface IEntityUpdatable : IEntity
{
    DateTime UpdatedAt { get; set; }
}