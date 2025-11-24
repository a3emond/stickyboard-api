namespace StickyBoard.Core.Models.Base;

public interface IEntity
{
    DateTime? CreatedAt => null;

    Guid? GetId()
    {
        return null;
    }
}