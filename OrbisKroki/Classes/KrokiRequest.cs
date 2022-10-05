using ESRI.ArcGIS.Geometry;
using Newtonsoft.Json;

namespace OrbisKroki.Classes
{
    public class KrokiRequest
    {
        public KrokiRequest()
        {
            LayerId = -1;
            MapLayerIndex = 0;
            KrokiName = null;
            Extent = null;
            Scale = -1;
            Expand = 1.0;
            Settings = null;
            OutputFileExtension = ContentTypeFileExtensionEnum.PNG;
        }
        [JsonProperty("layerId")]
        public int LayerId { get; set; }
        [JsonProperty("mapLayerIndex")]
        public int MapLayerIndex { get; set; }
        [JsonProperty("filterFieldName")]
        public string FilterFieldName { get; set; }
        [JsonProperty("filterValues")]
        public string FilterValues { get; set; }
        [JsonProperty("definitionExpression")]
        public string DefinitionExpression { get; set; }
        [JsonProperty("krokiName")]
        public string KrokiName { get; set; }
        [JsonProperty("settings")]
        public KrokiSettings Settings { get; set; }
        [JsonProperty("outputFileExtension")]
        public ContentTypeFileExtensionEnum OutputFileExtension { get; set; }
        [JsonProperty("extent")]
        public IEnvelope Extent { get; set; }
        [JsonProperty("scale")]
        public double Scale { get; set; }
        [JsonProperty("expand")]
        public double Expand { get; set; }
    }
}