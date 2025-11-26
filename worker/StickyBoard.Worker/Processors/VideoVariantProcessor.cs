using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Models.Attachments;
using StickyBoard.Core.Services.Attachments;
using StickyBoard.Worker.Utils;
using System.Diagnostics;
using System.Net.Http.Headers;
using StickyBoard.Core.Models;

namespace StickyBoard.Worker.Processors;

public static class VideoVariantProcessor
{
    public static async Task ProcessAsync(
        IAttachmentManager attachments,
        HttpClient http,
        MemoryStream input,
        Guid attachmentId,
        string storagePath,
        string? mime,
        CancellationToken ct)
    {
        var tempVideo = Path.GetTempFileName();
        var thumbImage = Path.GetTempFileName() + ".jpg";
        var previewImage = Path.GetTempFileName() + ".jpg";

        try
        {
            // Save local temp copy
            await File.WriteAllBytesAsync(tempVideo, input.ToArray(), ct);

            // Generate first-frame thumbnail (256px)
            await Run("ffmpeg",
                $"-i \"{tempVideo}\" -vf scale=256:-1 -frames:v 1 \"{thumbImage}\"", ct);

            await UploadImage(
                attachments, http, attachmentId, storagePath,
                AttachmentVariantType.Thumb,
                thumbImage,
                "image/jpeg",
                ct
            );

            // Generate preview (1024px)
            await Run("ffmpeg",
                $"-i \"{tempVideo}\" -vf scale=1024:-1 -frames:v 1 \"{previewImage}\"", ct);

            await UploadImage(
                attachments, http, attachmentId, storagePath,
                AttachmentVariantType.Preview,
                previewImage,
                "image/jpeg",
                ct
            );
        }
        finally
        {
            TryDelete(tempVideo);
            TryDelete(thumbImage);
            TryDelete(previewImage);
        }
    }

    private static async Task UploadImage(
        IAttachmentManager attachments,
        HttpClient http,
        Guid attachmentId,
        string originalPath,
        AttachmentVariantType type,
        string localImage,
        string mime,
        CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(localImage, ct);

        var uploadUrl = await attachments.GetVariantUploadUrlAsync(attachmentId, type, ct);

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mime);

        await http.PutAsync(uploadUrl, content, ct);

        var info = await ImageInfoUtils.GetAsync(localImage);

        var path = originalPath.Replace("/original/", $"/{type}/");

        await attachments.RegisterVariantAsync(new VariantRegisterRequest
        {
            AttachmentId = attachmentId,
            Variant = type,
            StoragePath = path,
            Mime = mime,
            ByteSize = bytes.Length,
            Width = info.Width,
            Height = info.Height,
            DurationMs = null,
            ChecksumSha256 = HashUtils.Sha256(bytes)
        }, ct);
    }

    private static async Task Run(string cmd, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var p = Process.Start(psi)!;
        await p.WaitForExitAsync(ct);

        if (p.ExitCode != 0)
        {
            var error = await p.StandardError.ReadToEndAsync();
            throw new Exception($"{cmd} failed: {error}");
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }
}
