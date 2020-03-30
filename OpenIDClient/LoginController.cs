using System.Net;
using Microsoft.AspNetCore.Mvc;
using OpenIDClient.Infrastructure;

namespace OpenIDClient
{
    [Route("api")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpGet("login")]
        public IActionResult Login()
        {
            // 3.1.2.  Authorization Endpoint
            const string authorizationEndpoint = "https://localhost:8080/connect/authorize";
	
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
}