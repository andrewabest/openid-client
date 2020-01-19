using Newtonsoft.Json;

namespace OpenIDClient.Models
{
    // Created by
    // https://app.quicktype.io/#l=cs&r=json2csharp
    //
    public class Key
    {
        [JsonProperty("kty")]
        public string Kty { get; set; }

        [JsonProperty("use")]
        public string Use { get; set; }

        [JsonProperty("kid")]
        public string Kid { get; set; }

        [JsonProperty("e")]
        public string E { get; set; }

        [JsonProperty("n")]
        public string N { get; set; }

        [JsonProperty("alg")]
        public string Alg { get; set; }
    }
}