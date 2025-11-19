namespace StickyBoard.Api.Models.Base
{
    public interface IVersionedEntity : IEntityUpdatable
    {
        int Version { get; set; }
    }
}