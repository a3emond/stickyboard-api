using StickyBoard.Api.Models;

namespace StickyBoard.Api.Common.Exceptions;

public sealed class ConflictException : DomainException
{
    // Primary constructor
    public ConflictException(
        string message,
        ErrorCode code = ErrorCode.CONFLICT,
        int statusCode = 409)
        : base(message, code, statusCode)
    {
    }



    // Parameterless defaults
    public ConflictException()
        : base("Conflict occurred.", ErrorCode.CONFLICT, 409)
    {
    }
}