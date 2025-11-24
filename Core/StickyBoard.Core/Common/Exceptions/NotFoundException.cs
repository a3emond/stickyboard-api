using System.Net;
using StickyBoard.Core.Models;

namespace StickyBoard.Core.Common.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message)
        : base(message, ErrorCode.NOT_FOUND, (int)HttpStatusCode.NotFound)
    {
    }
}