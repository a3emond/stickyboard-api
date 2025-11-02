using System.Net;
using StickyBoard.Api.Models;

namespace StickyBoard.Api.Common.Exceptions
{
    public sealed class NotFoundException : DomainException
    {
        public NotFoundException(string message)
            : base(message, ErrorCode.NOT_FOUND, (int)HttpStatusCode.NotFound) { }
    }
}