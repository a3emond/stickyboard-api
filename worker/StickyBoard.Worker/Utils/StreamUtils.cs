namespace StickyBoard.Worker.Utils;

public static class StreamUtils
{
    public static async Task<byte[]> ToArrayAsync(Stream input, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}