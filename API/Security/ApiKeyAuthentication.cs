using System.Security.Cryptography;
using System.Text;

namespace API.Security;

public static class ApiKeyAuthentication
{
    public const string EnvironmentVariable = "VEDASTRO_API_KEY";
    public const string HeaderName = "X-API-Key";

    public static bool IsExemptPath(string? path) =>
        string.Equals(path, "/api/version", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(path, "/api/version/", StringComparison.OrdinalIgnoreCase);

    public static bool IsAuthorized(string? configuredKey, string? suppliedKey)
    {
        if (string.IsNullOrEmpty(configuredKey) || string.IsNullOrEmpty(suppliedKey))
        {
            return false;
        }

        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedKey));
        return CryptographicOperations.FixedTimeEquals(configuredHash, suppliedHash);
    }
}
