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

            return Ok();
        }
    }
}