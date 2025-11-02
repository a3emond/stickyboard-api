using System.Net;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Common.Exceptions
{
    public sealed class ForbiddenException : DomainException
    {
        public ForbiddenException(string message = "Not allowed")
            : base(message, ErrorCode.FORBIDDEN, (int)HttpStatusCode.Forbidden) { }
    }
}