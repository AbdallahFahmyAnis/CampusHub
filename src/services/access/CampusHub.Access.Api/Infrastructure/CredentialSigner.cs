using System.Security.Cryptography;
using System.Text;

namespace CampusHub.Access.Api.Infrastructure;

public sealed class CredentialSigner(IConfiguration config)
{
    public string Sign(Guid credentialId, Guid enrollmentId, Guid courseId, string kind, long expUnix)
    {
        var payload = Canonical(credentialId, enrollmentId, courseId, kind, expUnix);
        var signature = Convert.ToHexString(Hmac(payload)).ToLowerInvariant();
        return $"{payload}.{signature}";
    }

    public bool TryVerify(string token, out Guid credentialId, out Guid enrollmentId, out Guid courseId, out string kind)
    {
        credentialId = Guid.Empty;
        enrollmentId = Guid.Empty;
        courseId = Guid.Empty;
        kind = string.Empty;

        var parts = token.Split('.');
        if (parts.Length != 6)
        {
            return false;
        }

        if (!Guid.TryParse(parts[0], out credentialId) ||
            !Guid.TryParse(parts[1], out enrollmentId) ||
            !Guid.TryParse(parts[2], out courseId) ||
            !long.TryParse(parts[4], out var expUnix))
        {
            return false;
        }

        kind = parts[3];
        var payload = Canonical(credentialId, enrollmentId, courseId, kind, expUnix);
        var expected = Convert.ToHexString(Hmac(payload)).ToLowerInvariant();
        if (parts[5].Length != expected.Length ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[5])))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
        {
            return false;
        }

        return true;
    }

    private static string Canonical(Guid credentialId, Guid enrollmentId, Guid courseId, string kind, long expUnix) =>
        $"{credentialId:N}.{enrollmentId:N}.{courseId:N}.{kind}.{expUnix}";

    private byte[] Hmac(string payload)
    {
        var key = Encoding.UTF8.GetBytes(config["Access:SigningKey"] ?? "campus-dev-qr-signing");
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
    }
}
