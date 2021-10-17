using Newtonsoft.Json;

namespace WheelchairBot
{
    public struct Configuration
    {
        [JsonProperty("token")]
        public string Token { get; private set; }
        [JsonProperty("prefix")]
        public string Prefix { get; private set; }
        [JsonProperty("apikey")]
        public string APIKey { get; private set; }
    }
}
