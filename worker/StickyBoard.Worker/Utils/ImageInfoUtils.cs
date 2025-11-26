using SixLabors.ImageSharp;

namespace StickyBoard.Worker.Utils;

public static class ImageInfoUtils
{
    public static async Task<(int Width, int Height)> GetAsync(string path)
    {
        await using var fs = File.OpenRead(path);
        var info = await Image.IdentifyAsync(fs);
        return (info!.Width, info.Height);
    }
}