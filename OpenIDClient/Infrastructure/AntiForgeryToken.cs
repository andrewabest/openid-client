using System;
using System.Security.Cryptography;
using System.Text;

namespace OpenIDClient.Infrastructure
{
    public class AntiForgeryToken
    {
        public static string Secret = "super-secret";

        // HMAC-Based CSRF Token Generation
        // https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html#hmac-based-token-pattern
        //
        public static string GetForCurrentUser()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return CreateToken(now.ToString());
        }

        private static string CreateToken(string currentTimeStamp)
        {
            var secretKeyBytes = Encoding.UTF8.GetBytes(Secret);
            var stringToSignBytes = Encoding.UTF8.GetBytes($"{UserProvider.Current.SessionId}{currentTimeStamp}");

            using var hmac = new HMACSHA256(secretKeyBytes);
            var signatureBytes = hmac.ComputeHash(stringToSignBytes);
            return $"{Convert.ToBase64String(signatureBytes)}:{currentTimeStamp}";
        }

        public static bool IsValid(string token)
        {
            var timestampLocation = token.LastIndexOf(':') + 1;
            var timestamp = token[timestampLocation..];

            if (DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp)).AddHours(1) < DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("Expired");
            }

            return CreateToken(timestamp).Equals(token);
        }
    }
}