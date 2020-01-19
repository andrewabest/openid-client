using Newtonsoft.Json;

namespace OpenIDClient.Models
{
    // Created by
    // https://app.quicktype.io/#l=cs&r=json2csharp
    //
    public class KeySet
    {
        [JsonProperty("keys")]
        public Key[] Keys { get; set; }
    }
}