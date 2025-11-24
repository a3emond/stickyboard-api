using System.Net;
using StickyBoard.Core.Models;

namespace StickyBoard.Core.Common.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Not allowed")
        : base(message, ErrorCode.FORBIDDEN, (int)HttpStatusCode.Forbidden)
    {
    }
}