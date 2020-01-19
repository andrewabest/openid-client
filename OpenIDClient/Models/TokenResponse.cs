using Microsoft.AspNetCore.Mvc;

namespace OpenIDClient.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string IdToken { get; set; }
        public string Error { get; set; }
        [FromQuery(Name = "error_description")]
        public string ErrorDescription { get; set; }
        [FromQuery(Name = "error_uri")]
        public string ErrorUri { get; set; }
    }
}