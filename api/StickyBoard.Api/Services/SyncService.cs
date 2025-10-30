using System.Text.Json;
using StickyBoard.Api.DTOs.Sync;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Repositories.SectionsAndTabs;

namespace StickyBoard.Api.Services
{
    public sealed class SyncService
    {
        private readonly OperationService _operations;
        private readonly WorkerJobService _worker;
        private readonly BoardRepository _boards;
        private readonly SectionRepository _sections;
        private readonly TabRepository _tabs;
        private readonly CardRepository _cards;
        private readonly FileRepository _files;
        private readonly ActivityRepository _activities;

        public SyncService(
            OperationService operations,
            WorkerJobService worker,
            BoardRepository boards,
            SectionRepository sections,
            TabRepository tabs,
            CardRepository cards,
            FileRepository files,
            ActivityRepository activities)
        {
            _operations = operations;
            _worker = worker;
            _boards = boards;
            _sections = sections;
            _tabs = tabs;
            _cards = cards;
            _files = files;
            _activities = activities;
        }

        // ------------------------------------------------------------
        // COMMIT (client → server)
        // ------------------------------------------------------------
        public async Task<SyncCommitResultDto> CommitAsync(Guid userId, SyncCommitRequestDto dto, CancellationToken ct)
        {
            var accepted = new List<Guid>();

            foreach (var op in dto.Operations)
            {
                Dictionary<string, object>? payload = null;

                if (!string.IsNullOrWhiteSpace(op.PayloadJson))
                {
                    payload = JsonSerializer.Deserialize<Dictionary<string, object>>(op.PayloadJson!);
                }

                var id = await _operations.AppendAsync(userId, new DTOs.Operations.CreateOperationDto
                {
                    DeviceId = dto.DeviceId,
                    Entity   = op.Entity,
                    EntityId = op.EntityId,
                    OpType   = op.OpType,
                    PayloadJson = payload,          
                    VersionPrev = op.VersionPrev,
                    VersionNext = op.VersionNext

                }, ct);

                accepted.Add(id);
            }

            await _worker.EnqueueAsync(
                JobKind.synccompactor,
                new { DeviceId = dto.DeviceId, UserId = userId },
                priority: 0, maxAttempts: 3, ct);

            return new SyncCommitResultDto
            {
                AcceptedCount = accepted.Count,
                OperationIds = accepted,
                ServerTime = DateTime.UtcNow
            };
        }



        // ------------------------------------------------------------
        // PULL (server → client)
        // ------------------------------------------------------------
        public async Task<SyncPullResponseDto> PullAsync(Guid userId, DateTime since, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var result = new SyncPullResponseDto { ServerTime = now };

            result.Boards = (await _boards.GetUpdatedSinceAsync(since, ct))?.Cast<object>().ToList() ?? [];
            result.Sections = (await _sections.GetUpdatedSinceAsync(since, ct))?.Cast<object>().ToList() ?? [];
            result.Tabs = (await _tabs.GetUpdatedSinceAsync(since, ct))?.Cast<object>().ToList() ?? [];
            result.Cards = (await _cards.GetUpdatedSinceAsync(since, ct))?.Cast<object>().ToList() ?? [];
            result.Files = (await _files.GetUpdatedSinceAsync(since, ct))?.Cast<object>().ToList() ?? [];
            result.Activities = (await _activities.GetUpdatedSinceAsync(since, ct))?.Cast<object>().ToList() ?? [];

            return result;
        }
    }
}
