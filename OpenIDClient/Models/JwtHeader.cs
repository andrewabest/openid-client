using Newtonsoft.Json;

namespace OpenIDClient.Models
{ 
    // Created by
    // https://app.quicktype.io/#l=cs&r=json2csharp
    //
    public class JwtHeader
    {
        [JsonProperty("alg")]
        public string Alg { get; set; }

        [JsonProperty("kid")]
        public string Kid { get; set; }

        [JsonProperty("typ")]
        public string Typ { get; set; }
    }
}