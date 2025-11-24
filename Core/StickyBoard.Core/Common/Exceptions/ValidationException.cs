using System.Net;
using StickyBoard.Core.Models;

namespace StickyBoard.Core.Common.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(string message)
        : base(message, ErrorCode.VALIDATION_ERROR, (int)HttpStatusCode.BadRequest)
    {
    }
}