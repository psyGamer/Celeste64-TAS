using System.Security.Cryptography;
using System.Text;

namespace TAS.Utils;

internal static class HashHelper {
    private static readonly HashAlgorithm hasher = MD5.Create();

    public static string ComputeHash(string text) {
        byte[] data = hasher.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(data).Replace("-", string.Empty);
    }
}
