using StickyBoard.Api.Models;

namespace StickyBoard.Api.Common.Exceptions;

public abstract class DomainException : Exception
{
    public ErrorCode Code { get; }
    public int StatusCode { get; }

    protected DomainException(string message, ErrorCode code, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

