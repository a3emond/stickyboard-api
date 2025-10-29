using System;
using StickyBoard.Api.Models.Enums;

namespace StickyBoard.Api.DTOs.Messages
{
    public sealed class MessageDto
    {
        public Guid Id { get; set; }
        public Guid? SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public MessageType Type { get; set; }
        public Guid? RelatedBoardId { get; set; }
        public Guid? RelatedOrganizationId { get; set; }
        public MessageStatus Status { get; set; } = MessageStatus.unread;
        public DateTime CreatedAt { get; set; }
    }

    public sealed class SendMessageDto
    {
        public Guid ReceiverId { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public MessageType Type { get; set; } = MessageType.direct;
        public Guid? RelatedBoardId { get; set; }
        public Guid? RelatedOrganizationId { get; set; }
    }

    public sealed class UpdateMessageStatusDto
    {
        public MessageStatus Status { get; set; } = MessageStatus.read;
    }
}