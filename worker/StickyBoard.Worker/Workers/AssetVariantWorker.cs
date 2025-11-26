using System.Text.Json;
using Microsoft.Extensions.Logging;
using StickyBoard.Core.Models;
using StickyBoard.Core.Models.Automation.Jobs;
using StickyBoard.Core.Services.Attachments;
using StickyBoard.Worker.Processors;

namespace StickyBoard.Worker.Workers;

public sealed class AssetVariantWorker : IWorkerHandler
{
    public WorkerJobKind Kind => WorkerJobKind.AssetVariant;

    private readonly IAttachmentManager _attachments;
    private readonly HttpClient _http;
    private readonly ILogger<AssetVariantWorker> _logger;

    public AssetVariantWorker(
        IAttachmentManager attachments,
        HttpClient http,
        ILogger<AssetVariantWorker> logger)
    {
        _attachments = attachments;
        _http = http;
        _logger = logger;
    }

    public async Task HandleAsync(WorkerJob job, CancellationToken ct)
    {
        if (job.Payload is null)
            throw new InvalidOperationException("AssetVariant job has no payload.");

        var root = job.Payload.RootElement;

        var attachmentId = root.GetProperty("attachmentId").GetGuid();
        var storagePath  = root.GetProperty("storagePath").GetString()!;
        var mime         = root.GetProperty("mime").GetString();

        _logger.LogInformation("AssetVariantWorker running for {AttachmentId}", attachmentId);

        var originalUrl = await _attachments.GetOriginalForProcessingAsync(attachmentId, ct);

        await using var buffer = new MemoryStream();

        using (var response = await _http.GetAsync(originalUrl, ct))
        {
            response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(buffer, ct);
        }

        buffer.Position = 0;

        if (mime?.StartsWith("image/") == true)
        {
            await ImageVariantProcessor.ProcessAsync(
                _attachments, _http, buffer, attachmentId, storagePath, mime, ct);
            return;
        }

        if (mime?.StartsWith("video/") == true)
        {
            await VideoVariantProcessor.ProcessAsync(
                _attachments, _http, buffer, attachmentId, storagePath, mime, ct);
            return;
        }

        await GenericVariantProcessor.ProcessAsync(
            _attachments, attachmentId, storagePath, mime, ct);
    }
}
