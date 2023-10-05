using Newtonsoft.Json;
using System.Collections.Generic;

namespace OrbisKrokiTest
{
    public class MapProperties
    {
        public MapProperties()
        {
            ClipEnabled = false;
            ClipExcludeLayers = new List<int>();
            BufferDistance = 0.0;
        }
        [JsonProperty("clipenabled")]
        public bool ClipEnabled { get; set; }
        [JsonProperty("clipexcludelayers")]
        public List<int> ClipExcludeLayers;
        [JsonProperty("bufferdistance")]
        public double BufferDistance;

    }
}
