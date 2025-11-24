using StickyBoard.Core.Models.Automation.RealTime;

namespace StickyBoard.Core.Services.Automation.Realtime;

public interface IRealtimePublisher
{
    Task PublishAsync(EventOutbox evt, CancellationToken ct);
}