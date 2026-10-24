using System.Security.Cryptography;
using System.Text;

namespace JobRadar.Application.Common;

/// <summary>Hash SHA-256 (hex) para tokens opacos persistidos — o token puro nunca vai ao banco.</summary>
public static class TokenHasher
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Comparação em tempo constante entre dois hashes.</summary>
    public static bool FixedTimeEquals(string hashA, string hashB) =>
        hashA.Length == hashB.Length &&
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashA), Encoding.UTF8.GetBytes(hashB));
}
