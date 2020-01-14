using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JWT;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenIDClient
{
    public static class UserProvider
    {
        private static User _user;
        public static User Current => _user ??= new User();
    }

    public class User
    {
        public User()
        {
            var id = new byte[20];
            RandomNumberGenerator.Create().GetBytes(id);
            Id = Convert.ToBase64String(id);
        }

        public string Id { get; }
    }

    public class AntiForgeryToken
    {
        public static string Secret = "super-secret";

        // HMAC-Based CSRF Token Generation
        // https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html#hmac-based-token-pattern
        //
        public static string GetForCurrentUser()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return CreateToken(now.ToString());
        }

        private static string CreateToken(string currentTimeStamp)
        {
            var secretKeyBytes = Encoding.UTF8.GetBytes(Secret);
            var stringToSignBytes = Encoding.UTF8.GetBytes($"{UserProvider.Current.Id}{currentTimeStamp}");

            using var hmac = new HMACSHA256(secretKeyBytes);
            var signatureBytes = hmac.ComputeHash(stringToSignBytes);
            return $"{Convert.ToBase64String(signatureBytes)}:{currentTimeStamp}";
        }

        public static bool IsValid(string token)
        {
            var timestampLocation = token.LastIndexOf(':');
            return GetForCurrentUser().Equals(token[^timestampLocation..^0]);
        }
    }

    [Route("api")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpGet("login")]
        public IActionResult Login()
        {
            // 3.1.2.  Authorization Endpoint
            const string authorizationEndpoint = "http://localhost:8080/connect/authorize";
	
            // 3.1.2.1.  Authentication Request
            var requestContent = 
                "scope=openid%20profile%20email&" +
                "response_type=code&" +
                "client_id=client&" +
                "redirect_uri=https%3A%2F%2Flocalhost%3A8090%2Fapi%2Fauthorize&" +
                $"state={WebUtility.UrlEncode(AntiForgeryToken.GetForCurrentUser())}";

            return Redirect($"{authorizationEndpoint}?{requestContent}");
        }
    }

    [Route("api")]
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        [HttpGet("authorize")]
        // 3.1.2.5.  Successful Authentication Response
        // 3.1.2.6.  Authentication Error Response
        public async Task<IActionResult> Authorize([FromQuery] AuthenticationResponse response)
        {
            // 3.1.2.7.  Authentication Response Validation
            // https://tools.ietf.org/html/rfc6749#section-10.12
            if (AntiForgeryToken.IsValid(response.State) == false)
            {
                return Forbid();
            }

            // 3.1.3.  Token Endpoint
            const string tokenEndpoint = "http://localhost:8080/connect/token";

            // 3.1.3.1.  Token Request
            var requestContent = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", response.Code),
                new KeyValuePair<string, string>("redirect_uri", "https://localhost:8090/api/authorize")
            };

            // https://tools.ietf.org/html/rfc6749#section-2.3.1
            var authorizationHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("client:secret")));

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = authorizationHeader;

            var result = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestContent));

            result.EnsureSuccessStatusCode();

            // 3.1.3.3.  Successful Token Response
            // https://tools.ietf.org/html/rfc6749#section-5.1
            // 
            var serializedTokenResponse = await result.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResult>(serializedTokenResponse, new JsonSerializerSettings()
            {
                ContractResolver = new UnderscorePropertyNamesContractResolver()
            });

            // 3.1.3.5.  Token Response Validation
            // Install-Package JWT
            var idTokenValidationResult = ValidateIdToken(tokenResponse.IdToken);
            if (idTokenValidationResult.Succeeded == false)
            {
                // TODO: Verify correct response
                // Note: The spec doesn't really say what response codes should be returned when things like validation fail.
                return ValidationProblem();
            }

            // TODO: any verification of token response.

            return Ok(tokenResponse);
        }

        private TokenValidationResult ValidateIdToken(string token)
        {
            IJsonSerializer serializer = new JsonNetSerializer();
            IDateTimeProvider provider = new UtcDateTimeProvider();
            IJwtValidator validator = new JwtValidator(serializer, provider);
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

            try
            {
                var json = decoder.Decode(token, "secret", verify: true);

                // Verify client / issuer / etc

                return new TokenValidationResult {Succeeded = true};
            }
            catch (TokenExpiredException)
            {
                return new TokenValidationResult {Message = "Token has expired"};
            }
            catch (SignatureVerificationException)
            {
                return new TokenValidationResult {Message = "Token has invalid signature"};
            }
        }
    }

    public class UnderscorePropertyNamesContractResolver : DefaultContractResolver  
    {
        public UnderscorePropertyNamesContractResolver() : base()
        {
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            return Regex.Replace(propertyName, @"(\w)([A-Z])", "$1_$2").ToLower();
        }
    }

    public class TokenValidationResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }

    public class TokenResult
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string IdToken { get; set; }
    }

    public class AuthenticationResponse
    {
        public string Code { get; set; }
        public string Scope { get; set; }
        public string State { get; set; }

        public string Error { get; set; }

        [FromQuery(Name = "error_description")]
        public string ErrorDescription { get; set; }

        [FromQuery(Name = "error_uri")]
        public string ErrorUri { get; set; }
    }
}