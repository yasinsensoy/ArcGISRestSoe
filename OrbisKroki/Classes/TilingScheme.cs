using ESRI.ArcGIS.Geometry;

namespace OrbisKroki.Classes
{
    public class TilingScheme
    {
        /// <summary>
        /// tiling scheme file path
        /// </summary>
        public string Path { get; set; }
        public string RestResponseArcGISJson { get; set; }
        public string RestResponseArcGISPJson { get; set; }
        public int WKID { get; set; }
        public string WKT { get; set; }
        public int TileCols { get; set; }
        public int TileRows { get; set; }
        /// <summary>
        /// PNG/PNG32/PNG24/PNG8/JPEG/JPG/Mixed
        /// </summary>
        public ImageFormat CacheTileFormat { get; set; }
        /// <summary>
        /// png=0,jpg=XX
        /// </summary>
        public int CompressionQuality { get; set; }
        public Point TileOrigin { get; set; }
        public LODInfo[] LODs { get; set; }
        public string LODsJson { get; set; }
        public IEnvelope InitialExtent { get; set; }
        public IEnvelope FullExtent { get; set; }
        public StorageFormat StorageFormat { get; set; }
        public int PacketSize { get; set; }
        public int DPI { get; set; }
    }

    public class LODInfo
    {
        public int LevelID { get; set; }
        public double Scale { get; set; }
        public double Resolution { get; set; }
    }

    public enum StorageFormat
    {
        esriMapCacheStorageModeExploded,
        esriMapCacheStorageModeCompact,
        unknown//ArcGISTiledMapService doesn't expose this property
    }

    public enum ImageFormat
    {
        PNG,
        PNG32,
        PNG24,
        PNG8,
        JPG,
        JPEG,
        MIXED
    }
}
