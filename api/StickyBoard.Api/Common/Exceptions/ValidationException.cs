using System.Net;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Common.Exceptions
{
    public sealed class ValidationException : DomainException
    {
        public ValidationException(string message)
            : base(message, ErrorCode.VALIDATION_ERROR, (int)HttpStatusCode.BadRequest) { }
    }
}