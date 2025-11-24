namespace StickyBoard.Core.Models.Base;

public interface IVersionedEntity : IEntityUpdatable
{
    int Version { get; set; }
}