using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using OpenIDClient.Extensions;
using OpenIDClient.Infrastructure;
using OpenIDClient.Models;

namespace OpenIDClient
{
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
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(serializedTokenResponse, new JsonSerializerSettings()
            {
                ContractResolver = new UnderscorePropertyNamesContractResolver()
            });

            // 3.1.3.5.  Token Response Validation
            // 3.1.3.7.  ID Token Validation
            // https://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation
            var idTokenValidationResult = await ValidateIdToken(tokenResponse.IdToken);

            // 3.1.3.8.  Access Token Validation
            // https://openid.net/specs/openid-connect-core-1_0.html#CodeFlowTokenValidation
            var accessTokenValidationResult = ValidateAccessToken(tokenResponse.IdToken, tokenResponse.AccessToken);

            if (idTokenValidationResult.Succeeded == false || accessTokenValidationResult.Succeeded == false)
            {
                return ValidationProblem();
            }

            // TODO: any verification of token response.

            return Ok(tokenResponse);
        }

        private enum TokenSegments
        {
            Header = 0,
            Body,
            Signature
        }

        // 3.1.3.7.  ID Token Validation - 6
        // The Client MUST validate the signature of all other ID Tokens according to JWS [JWS] using the algorithm specified in the JWT alg Header Parameter.
        // The Client MUST use the keys provided by the Issuer
        private async Task<TokenValidationResult> ValidateIdToken(string idToken)
        {
            try
            {
                // https://tools.ietf.org/html/draft-ietf-jose-json-web-key-41#section-4
                // https://tools.ietf.org/html/draft-ietf-jose-json-web-algorithms-40#section-6.3
                // JWT Structure https://tools.ietf.org/html/rfc7519#section-3
                // We are working with JWS, not JWE.

                var rawHeader = DecodeSegment(idToken, TokenSegments.Header);
                var rawBody = DecodeSegment(idToken, TokenSegments.Body);
                var rawSignature = WebEncoders.Base64UrlDecode(idToken.Split('.')[(int)TokenSegments.Signature]);
                var header = JsonConvert.DeserializeObject<JwtHeader>(rawHeader, Converter.Settings);
                var body = JsonConvert.DeserializeObject<JwtBody>(rawBody, Converter.Settings);

                // https://openid.net/specs/openid-connect-discovery-1_0.html#ProviderMetadata
                // https://openid.net/specs/openid-connect-discovery-1_0.html#ProviderConfig
                const string introspectionEndpoint = "http://localhost:8080/.well-known/openid-configuration";
                var configurationJson = await new HttpClient().GetStringAsync(introspectionEndpoint);
                var configuration =
                    JsonConvert.DeserializeObject<OpenIDConfiguration>(configurationJson, Converter.Settings);

                // 3.1.3.7.  ID Token Validation
                // https://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation

                body.Iss.MustEqual(configuration.Issuer, "Invalid issuer encountered");
                body.Aud.MustEqual("client", "Invalid audience encountered");
                header.Alg.MustEqual("RS256", "Unsupported algorithm specified");

                // IETF JWS Draft 
                // 5.2. Message Signature or MAC Validation
                // https://tools.ietf.org/html/draft-ietf-jose-json-web-signature-41#section-5.2

                var keysetJson = await new HttpClient().GetStringAsync(configuration.JwksUri);
                var keyset = JsonConvert.DeserializeObject<KeySet>(keysetJson, Converter.Settings);

                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Exponent = WebEncoders.Base64UrlDecode(keyset.Keys[0].E),
                    Modulus = WebEncoders.Base64UrlDecode(keyset.Keys[0].N),
                });

                var canonicalPayload =
                    Encoding.ASCII.GetBytes(
                        $"{WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawHeader))}." +
                        $"{WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawBody))}");

                var hash = SHA256.Create().ComputeHash(canonicalPayload);

                // https://tools.ietf.org/html/rfc3447#section-8.2.1
                var result = rsa.VerifyHash(hash, rawSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    
                if (!result) throw new InvalidOperationException("The supplied signature does not match the calculated signature");

                if (DateTimeOffset.FromUnixTimeSeconds(body.Exp) <= DateTimeOffset.UtcNow)
                {
                    throw new InvalidOperationException("Token has expired");
                }

                return new TokenValidationResult {Succeeded = true};
            }
            catch (Exception)
            {
                return new TokenValidationResult {Message = "Token validation failed"};
            }
        }

        // 3.1.3.8.  Access Token Validation
        // 3.2.2.9.  Access Token Validation
        // https://openid.net/specs/openid-connect-core-1_0.html#ImplicitTokenValidation
        private static TokenValidationResult ValidateAccessToken(string idToken, string accessToken)
        {
            var rawBody = DecodeSegment(idToken, TokenSegments.Body);
            var body = JsonConvert.DeserializeObject<JwtBody>(rawBody, Converter.Settings);

            var octets = Encoding.ASCII.GetBytes(accessToken);
            var hash = SHA256.Create().ComputeHash(octets);
            var encoded = WebEncoders.Base64UrlEncode(hash[..(hash.Length / 2)]);

            if (encoded.Equals(body.AtHash) == false) throw new InvalidOperationException("The supplied signature does not match the calculated signature");

            return new TokenValidationResult { Succeeded = true };
        }

        private static string DecodeSegment(string token, TokenSegments desiredSegment)
        {
            var tokenSegments = token.Split(".");
            if (tokenSegments.Length != 3) throw new InvalidOperationException("Unexpected token format encountered");

            var target = tokenSegments[(int)desiredSegment];

            // NOTE: WebEncoders.Base64UrlDecode deals with IDSrv truncating padding from encoded tokens
            var unencodedBytes = WebEncoders.Base64UrlDecode(target);
            var segment = Encoding.UTF8.GetString(unencodedBytes);

            return segment;
        }
    }
}