using StickyBoard.Api.DTOs.Files;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Models.FilesAndOps;
using StickyBoard.Api.Repositories;
using System.Text.Json;
using File = StickyBoard.Api.Models.FilesAndOps.File;

namespace StickyBoard.Api.Services
{
    public sealed class FileService
    {
        private readonly FileRepository _files;
        private readonly BoardRepository _boards;
        private readonly CardRepository _cards;
        private readonly PermissionRepository _permissions;

        public FileService(
            FileRepository files,
            BoardRepository boards,
            CardRepository cards,
            PermissionRepository permissions)
        {
            _files = files;
            _boards = boards;
            _cards = cards;
            _permissions = permissions;
        }

        // ------------------------------------------------------------
        // PERMISSION HELPERS
        // ------------------------------------------------------------
        private async Task EnsureCanEditAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct)
                        ?? throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor))
                throw new UnauthorizedAccessException("User not allowed to modify files on this board.");
        }

        private async Task EnsureCanViewAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            var board = await _boards.GetByIdAsync(boardId, ct)
                        ?? throw new KeyNotFoundException("Board not found.");

            var isOwner = board.OwnerId == userId;
            var role = (await _permissions.GetAsync(boardId, userId, ct))?.Role;

            if (!(isOwner || role is BoardRole.owner or BoardRole.editor or BoardRole.viewer))
                throw new UnauthorizedAccessException("User not allowed to view files on this board.");
        }

        private async Task<Guid> ResolveBoardIdAsync(Guid? boardId, Guid? cardId, CancellationToken ct)
        {
            if (boardId.HasValue)
                return boardId.Value;

            if (cardId.HasValue)
            {
                var card = await _cards.GetByIdAsync(cardId.Value, ct)
                           ?? throw new KeyNotFoundException("Card not found.");
                return card.BoardId;
            }

            throw new ArgumentException("Either BoardId or CardId must be provided.");
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------
        public async Task<Guid> CreateAsync(Guid ownerId, CreateFileDto dto, CancellationToken ct)
        {
            var boardId = await ResolveBoardIdAsync(dto.BoardId, dto.CardId, ct);
            await EnsureCanEditAsync(ownerId, boardId, ct);

            var entity = new File
            {
                OwnerId = ownerId,
                BoardId = dto.BoardId,
                CardId = dto.CardId,
                StorageKey = dto.StorageKey,
                FileName = dto.FileName,
                MimeType = dto.MimeType,
                SizeBytes = dto.SizeBytes,
                Meta = JsonDocument.Parse(dto.MetaJson ?? "{}"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _files.CreateAsync(entity, ct);
        }

        // ------------------------------------------------------------
        // READ
        // ------------------------------------------------------------
        public async Task<IEnumerable<File>> GetByBoardAsync(Guid userId, Guid boardId, CancellationToken ct)
        {
            await EnsureCanViewAsync(userId, boardId, ct);
            return await _files.GetByBoardAsync(boardId, ct);
        }

        public async Task<IEnumerable<File>> GetByCardAsync(Guid userId, Guid cardId, CancellationToken ct)
        {
            var card = await _cards.GetByIdAsync(cardId, ct)
                        ?? throw new KeyNotFoundException("Card not found.");
            await EnsureCanViewAsync(userId, card.BoardId, ct);
            return await _files.GetByCardAsync(cardId, ct);
        }

        public async Task<File?> GetAsync(Guid userId, Guid fileId, CancellationToken ct)
        {
            var file = await _files.GetByIdAsync(fileId, ct);
            if (file is null)
                return null;

            var boardId = await ResolveBoardIdAsync(file.BoardId, file.CardId, ct);
            await EnsureCanViewAsync(userId, boardId, ct);

            return file;
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        public async Task<bool> DeleteAsync(Guid userId, Guid fileId, CancellationToken ct)
        {
            var file = await _files.GetByIdAsync(fileId, ct);
            if (file is null)
                return false;

            var boardId = await ResolveBoardIdAsync(file.BoardId, file.CardId, ct);
            await EnsureCanEditAsync(userId, boardId, ct);

            return await _files.DeleteAsync(fileId, ct);
        }
    }
}
