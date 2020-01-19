using Newtonsoft.Json;

namespace OpenIDClient.Models
{
    // Created by
    // https://app.quicktype.io/#l=cs&r=json2csharp
    //
    public class JwtBody
    {
        [JsonProperty("nbf")]
        public long Nbf { get; set; }

        [JsonProperty("exp")]
        public long Exp { get; set; }

        [JsonProperty("iss")]
        public string Iss { get; set; }

        [JsonProperty("aud")]
        public string Aud { get; set; }

        [JsonProperty("iat")]
        public long Iat { get; set; }

        [JsonProperty("at_hash")]
        public string AtHash { get; set; }

        [JsonProperty("sid")]
        public string Sid { get; set; }

        [JsonProperty("sub")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Sub { get; set; }

        [JsonProperty("auth_time")]
        public long AuthTime { get; set; }

        [JsonProperty("idp")]
        public string Idp { get; set; }

        [JsonProperty("amr")]
        public string[] Amr { get; set; }
    }
}