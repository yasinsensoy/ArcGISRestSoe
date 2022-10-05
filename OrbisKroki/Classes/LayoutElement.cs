using Newtonsoft.Json;

namespace OrbisKroki.Classes
{
    public class LayoutElement
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
