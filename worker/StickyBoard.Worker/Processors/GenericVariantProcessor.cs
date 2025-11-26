using StickyBoard.Core.DTOs.Attachments;
using StickyBoard.Core.Services.Attachments;
using StickyBoard.Worker.Utils;
using System.Diagnostics;
using System.Net.Http.Headers;
using StickyBoard.Core.Models;

namespace StickyBoard.Worker.Processors;

public static class GenericVariantProcessor
{
    public static async Task ProcessAsync(
        IAttachmentManager attachments,
        Guid attachmentId,
        string storagePath,
        string? mime,
        CancellationToken ct)
    {
        if (mime == "application/pdf")
        {
            await ProcessPdfAsync(attachments, attachmentId, storagePath, ct);
            return;
        }

        // fallback icon
        var icon = IconFor(mime);

        await attachments.RegisterVariantAsync(new VariantRegisterRequest
        {
            AttachmentId = attachmentId,
            Variant = AttachmentVariantType.Thumb,
            StoragePath = $"system/icons/{icon}.png",
            Mime = "image/png",
            ByteSize = null,
            Width = 256,
            Height = 256
        }, ct);
    }

    private static async Task ProcessPdfAsync(
        IAttachmentManager attachments,
        Guid attachmentId,
        string storagePath,
        CancellationToken ct)
    {
        var download = await attachments.GetOriginalForProcessingAsync(attachmentId, ct);

        var tempPdf = Path.GetTempFileName() + ".pdf";
        var output = Path.GetTempFileName() + ".png";

        using (var client = new HttpClient())
        {
            var pdfBytes = await client.GetByteArrayAsync(download, ct);
            await File.WriteAllBytesAsync(tempPdf, pdfBytes, ct);
        }

        await Run("pdftoppm", $"-png -f 1 -l 1 \"{tempPdf}\" \"{output}\"", ct);

        var png = output + "-1.png";
        var data = await File.ReadAllBytesAsync(png, ct);

        var uploadUrl = await attachments.GetVariantUploadUrlAsync(
            attachmentId,
            AttachmentVariantType.Preview,
            ct
        );

        using var content = new ByteArrayContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        await new HttpClient().PutAsync(uploadUrl, content, ct);

        var info = await ImageInfoUtils.GetAsync(png);

        var newPath = storagePath.Replace("/original/", "/Preview/");

        await attachments.RegisterVariantAsync(new VariantRegisterRequest
        {
            AttachmentId = attachmentId,
            Variant = AttachmentVariantType.Preview,
            StoragePath = newPath,
            Mime = "image/png",
            ByteSize = data.Length,
            Width = info.Width,
            Height = info.Height,
            ChecksumSha256 = HashUtils.Sha256(data)
        }, ct);

        TryDelete(tempPdf);
        TryDelete(png);
    }

    private static async Task Run(string cmd, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardError = true
        };

        using var p = Process.Start(psi)!;
        await p.WaitForExitAsync(ct);

        if (p.ExitCode != 0)
        {
            var error = await p.StandardError.ReadToEndAsync();
            throw new Exception($"{cmd} failed: {error}");
        }
    }

    private static string IconFor(string? mime)
    {
        if (string.IsNullOrWhiteSpace(mime)) return "file";

        return mime switch
        {
            var m when m.Contains("zip")  => "zip",
            var m when m.Contains("word") => "doc",
            var m when m.Contains("text") => "text",
            var m when m.Contains("excel")=> "xls",
            _ => "file"
        };
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }
}
