using StickyBoard.Core.Models;

namespace StickyBoard.Core.Common.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message, ErrorCode code, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public ErrorCode Code { get; }
    public int StatusCode { get; }
}