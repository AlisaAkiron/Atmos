using System.Security.Cryptography;

namespace Atmos.Common.Utils;

public static class RandomUtils
{
    private static ReadOnlySpan<char> Alphabet => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string GetRandomAlphabetString(int length)
    {
        return RandomNumberGenerator.GetString(Alphabet, length);
    }
}
