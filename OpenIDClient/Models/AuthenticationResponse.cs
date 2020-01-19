using Microsoft.AspNetCore.Mvc;

namespace OpenIDClient.Models
{
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