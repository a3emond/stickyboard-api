using System.Security.Cryptography;

namespace StickyBoard.Worker.Utils;

public static class HashUtils
{
    public static byte[] Sha256(byte[] input)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(input);
    }
}