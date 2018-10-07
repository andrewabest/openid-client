using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OpenIDClient
{
    [Route("api")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpGet("login")]
        public IActionResult Login()
        {
            const string  authorizationEndpoint = "http://localhost:8080/connect/authorize";
	
            const string requestContent = 
                "scope=openid%20profile%20email&" +
                "response_type=code&" +
                "client_id=client&" +
                "redirect_uri=https%3A%2F%2Flocalhost%3A8090%2Fapi%2Fauthorize";

            return Redirect($"{authorizationEndpoint}?{requestContent}");
        }
    }

    [Route("api")]
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        [HttpGet("authorize")]
        public IActionResult Authorize([FromQuery] AuthenticationCodeResponse response)
        {
            return Ok();
        }
    }

    public class AuthenticationCodeResponse
    {
        public string Code { get; set; }
        public string Scope { get; set; }
        public string SessionState { get; set; }

        public string Error { get; set; }
        public string ErrorDescription { get; set; }
        public string ErrorUri { get; set; }
    }
}