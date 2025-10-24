namespace StickyBoard.Api.Models.Base;

public interface IEntity
{
    //Guid Id { get; set; }  // -removed to accomodate refresh token which has the hashed token as PK
    DateTime CreatedAt { get; set; }
}