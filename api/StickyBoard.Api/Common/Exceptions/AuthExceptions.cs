using System.Net;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Common.Exceptions
{
    public sealed class AuthInvalidException : DomainException
    {
        public AuthInvalidException(string message = "Invalid credentials")
            : base(message, ErrorCode.AUTH_INVALID, (int)HttpStatusCode.Unauthorized) { }
    }


    public sealed class AuthExpiredException : DomainException
    {
        public AuthExpiredException(string message = "Token expired")
            : base(message, ErrorCode.AUTH_EXPIRED, (int)HttpStatusCode.Unauthorized) { }
    }
}