using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;

namespace OpenIDClient.Models
{
    public class UnderscorePropertyNamesContractResolver : DefaultContractResolver  
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return Regex.Replace(propertyName, @"(\w)([A-Z])", "$1_$2").ToLower();
        }
    }
}