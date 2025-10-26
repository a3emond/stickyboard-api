namespace StickyBoard.Api.Models.Base;

public interface IEntity
{
    Guid? GetId() => null;
    DateTime? CreatedAt => null;
}
