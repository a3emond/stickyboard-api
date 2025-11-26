using System.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Models;
using StickyBoard.Core.Services.Attachments;

namespace StickyBoard.Worker.Processors;

public static class ImageVariantProcessor
{
    public static async Task ProcessAsync(
        IAttachmentManager attachments,
        HttpClient http,
        MemoryStream input,
        Guid attachmentId,
        string storagePath,
        string mime,
        CancellationToken ct)
    {
        input.Position = 0;

        using var img = await Image.LoadAsync(input, ct);

        await GenerateAsync(
            attachments, http, img, attachmentId, storagePath,
            AttachmentVariantType.Thumb,
            "image/png", 256, ct
        );

        await GenerateAsync(
            attachments, http, img, attachmentId, storagePath,
            AttachmentVariantType.Preview,
            "image/jpeg", 1024, ct
        );
    }

    private static async Task GenerateAsync(
        IAttachmentManager attachments,
        HttpClient http,
        Image source,
        Guid attachmentId,
        string originalPath,
        AttachmentVariantType type,
        string mime,
        int maxSize,
        CancellationToken ct)
    {
        using var variant = source.Clone(x =>
            x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxSize, maxSize)
            })
        );

        await using var ms = new MemoryStream();

        if (mime == "image/png")
            await variant.SaveAsPngAsync(ms, ct);
        else
            await variant.SaveAsJpegAsync(ms, ct);

        ms.Position = 0;

        var uploadUrl = await attachments.GetVariantUploadUrlAsync(
            attachmentId, type, ct);

        using (var content = new StreamContent(ms))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(mime);
            await http.PutAsync(uploadUrl, content, ct);
        }

        var variantPath =
            originalPath.Replace("/original/", $"/{type}/", StringComparison.Ordinal);

        await attachments.RegisterVariantAsync(new VariantRegisterRequest
        {
            AttachmentId = attachmentId,
            Variant = type,
            StoragePath = variantPath,
            Mime = mime,
            ByteSize = ms.Length,
            Width = variant.Width,
            Height = variant.Height,
            DurationMs = null,
            ChecksumSha256 = null
        }, ct);
    }
}
