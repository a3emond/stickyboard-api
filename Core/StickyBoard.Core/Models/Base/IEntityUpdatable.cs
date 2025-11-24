namespace StickyBoard.Core.Models.Base;

public interface IEntityUpdatable : IEntity
{
    DateTime UpdatedAt { get; set; }
}