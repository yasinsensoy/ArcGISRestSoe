// Copyright 2018 ESRI
// 
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
// 
// You may freely redistribute and use this sample code, with or
// without modification, provided you include the original copyright
// notice and use restrictions.
// 
// See the use restrictions at <your Enterprise SDK install location>/userestrictions.txt.
// 

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Output;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.SOESupport;
using Newtonsoft.Json;
using OrbisKroki.Classes;
using OrbisKroki.Functionalities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

//This is REST SOE template of Enterprise SDK

//TODO: sign the project (project properties > signing tab > sign the assembly)
//      this is strongly suggested if the dll will be registered using regasm.exe <your>.dll /codebase


namespace OrbisKroki
{
    [ComVisible(true)]
    [Guid("8cee4b75-9ba9-40f1-b54a-a35a68a1b67a")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",
        AllCapabilities = "GetInfo,ExportLayout",
        DefaultCapabilities = "GetInfo,ExportLayout",
        Description = "Orbis Projesinde Servis üzerinden, Harita Krokileri almak için geliştirilmiştir.",
        DisplayName = "SOE Orbis Kroki Projesi",
        HasManagerPropertiesConfigurationPane = true,
        Properties = "useDynamicTile=false;useProxy=false;proxy=http://orbis.ogm.gov.tr/orbis/orbis/proxy?;outputPath=\\\\ogmdata.ogm.gov.tr\\Orbis\\YeniOrbisRapor\\orbis_harita\\ORBIS\\orbisoutput;krokiRootPath=\\\\ogmdata.ogm.gov.tr\\Orbis\\YeniOrbisRapor\\orbis_harita\\ORBIS\\Kroki",
        SupportsREST = true,
        SupportsSOAP = false,
        SupportsSharedInstances = false)]
    public class OrbisKroki : IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        private readonly string soe_name;
        private readonly Dictionary<int, IFeatureClass> idBasedIFeatureClasses = new Dictionary<int, IFeatureClass>();
        private readonly CultureInfo culture = new CultureInfo("en-us");
        private readonly IRESTRequestHandler reqHandler;
        private const string c_CapabilityGetInfo = "GetInfo";
        private const string c_CapabilityExportLayout = "ExportLayout";
        private Dictionary<string, string> environmentProperties = new Dictionary<string, string>();
        private IServerObjectHelper serverObjectHelper;
        private ServerLogger logger;
        private TilingScheme _tilingScheme = null;
        private IMapServer3 ms;
        private IMapServerDataAccess mapServerDataAccess;
        private IMapLayerInfos layerInfos;
        private IServerEnvironment2 serverEnvironment;
        private bool useDynamicTile = false;
        private bool useProxy = false;
        private string krokiRootPath = @"\\ogmdata.ogm.gov.tr\Orbis\YeniOrbisRapor\orbis_harita\ORBIS\Kroki";
        private string outputPath = @"\\ogmdata.ogm.gov.tr\Orbis\YeniOrbisRapor\orbis_harita\ORBIS\orbisoutput"; // C:\arcgisserver\directories\arcgisoutput
        private string proxy = "http://orbis.ogm.gov.tr/orbis/orbis/proxy?";
        private string restUrl;
        private string cfgName;
        private string cfgType;

        public OrbisKroki()
        {
            soe_name = GetType().Name;
            logger = new ServerLogger();
            reqHandler = new SoeRestImpl(soe_name, CreateRestSchema());
        }

        #region IServerObjectExtension Members
        public void Init(IServerObjectHelper pSOH)
        {
            serverObjectHelper = pSOH;
            logger = new ServerLogger();
            mapServerDataAccess = (IMapServerDataAccess)serverObjectHelper.ServerObject;
            ms = (IMapServer3)mapServerDataAccess;
            IMapServerInfo mapServerInfo = ms.GetServerInfo(ms.DefaultMapName);
            layerInfos = mapServerInfo.MapLayerInfos;
            environmentProperties = GetEnvironmentProperties();
            restUrl = environmentProperties["RestURL"].ToLower(culture).
                Replace(":6080", "").
                Replace(":6484", "").
                Replace("cbsogm.ogm.gov.tr", "gis.ogm.gov.tr").
                Replace("cbsogmpassive.ogm.gov.tr", "gis.ogm.gov.tr").
                Replace("/arcgis", "/server");
            cfgName = environmentProperties["CfgName"];
            cfgType = environmentProperties["CfgType"];
            logger.LogMessage(ServerLogger.msgType.infoStandard, soe_name + ".init()", 200, "Initialized " + soe_name + " SOE.");
        }

        public void Shutdown()
        {
            logger.LogMessage(ServerLogger.msgType.infoStandard, soe_name + ".init()", 200, "Shutting down " + soe_name + " SOE.");
            serverObjectHelper = null;
            logger = null;
            mapServerDataAccess = null;
            layerInfos = null;
        }

        public TilingScheme TilingScheme => _tilingScheme ?? ReadGoogleMapsTilingScheme(out _tilingScheme);

        public string GetServiceKrokiPath => System.IO.Path.Combine(krokiRootPath, cfgName);

        public string GetPhysicalRestOutputFolder => System.IO.Path.Combine(outputPath, $"{cfgName}_{cfgType}");

        public string GetServiceRestOutputUrlBase => $"{(useProxy ? proxy : "")}{restUrl}/directories/{System.IO.Path.GetFileName(outputPath)}/{cfgName}_{cfgType}";

        public string GetServiceRestUrlBase => $"{restUrl}/services/{cfgName}/{cfgType}/";
        #endregion

        #region IObjectConstruct Members
        public void Construct(IPropertySet props)
        {
            bool.TryParse((string)props.GetProperty("useDynamicTile"), out useDynamicTile);
            bool.TryParse((string)props.GetProperty("useProxy"), out useProxy);
            outputPath = (string)props.GetProperty("outputPath");
            krokiRootPath = (string)props.GetProperty("krokiRootPath");
            proxy = (string)props.GetProperty("proxy");
        }
        #endregion

        #region IRESTRequestHandler Members

        public string GetSchema()
        {
            return reqHandler.GetSchema();
        }

        public byte[] HandleRESTRequest(string Capabilities, string resourceName, string operationName, string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return reqHandler.HandleRESTRequest(Capabilities, resourceName, operationName, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        #endregion

        private RestResource CreateRestSchema()
        {
            RestResource soeResource = new RestResource(soe_name, false, RootResHandler, c_CapabilityGetInfo);

            RestOperation getLayoutDocumentsOperation = new RestOperation("GetLayoutDocuments", new string[] { "returnGeneral", "layerId" }, new string[] { "json" }, GetLayoutDocumentsOperationHandler, c_CapabilityGetInfo);
            soeResource.operations.Add(getLayoutDocumentsOperation);

            RestOperation getFeatureLayerExtentOperation = new RestOperation("GetFeatureLayerExtent", new string[] { "layerId" }, new string[] { "json" }, GetFeatureLayerExtentOperationHandler, c_CapabilityGetInfo);
            soeResource.operations.Add(getFeatureLayerExtentOperation);

            RestOperation findNearestFeaturesOperation = new RestOperation("FindNearestFeatures", new string[] { "layerId", "location", "distance" }, new string[] { "json" }, FindNearestFeaturesOperationHandler, c_CapabilityGetInfo);
            soeResource.operations.Add(findNearestFeaturesOperation);

            RestOperation exportRootLayoutOperation = new RestOperation("ExportLayoutRoot", new string[] { "layerId", "mapLayerIndex", "krokiName", "filterFieldName", "filterValues", "definitionExpression", "outputFileExtension", "extent", "scale", "expand", "settings" }, new string[] { "json" }, ExportLayoutOperationHandler, c_CapabilityExportLayout);
            soeResource.operations.Add(exportRootLayoutOperation);

            RestOperation exportGeneralLayoutOperation = new RestOperation("ExportGeneralLayout", new string[] { "layerId", "mapLayerIndex", "krokiName", "filterFieldName", "filterValues", "definitionExpression", "outputFileExtension", "extent", "scale", "expand", "settings" }, new string[] { "json" }, ExportLayoutOperationHandler, c_CapabilityExportLayout);
            soeResource.operations.Add(exportGeneralLayoutOperation);

            RestOperation getTileImageOperation = new RestOperation("GetTileImage", new string[] { "level", "row", "column" }, new string[] { "image/png" }, GetTileImageOperationHandler, c_CapabilityGetInfo);
            soeResource.operations.Add(getTileImageOperation);

            RestOperation exportLayerLayoutOperation = new RestOperation("ExportLayout", new string[] { "krokiName", "mapLayerIndex", "filterFieldName", "filterValues", "definitionExpression", "outputFileExtension", "extent", "scale", "expand", "settings" }, new string[] { "json" }, ExportLayoutOperationHandler, c_CapabilityExportLayout);
            RestResource customLayerResource = new RestResource("CustomLayers", true, true, CustomLayerHandler, c_CapabilityGetInfo);
            customLayerResource.operations.Add(exportLayerLayoutOperation);
            soeResource.resources.Add(customLayerResource);
            return soeResource;
        }

        #region Resource Handlers
        private byte[] RootResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = null;
            CustomLayerInfo[] layerInfos = GetCustomLayerInfos();
            JsonObject[] jos = new JsonObject[layerInfos.Length];
            for (int i = 0; i < layerInfos.Length; i++)
                jos[i] = layerInfos[i].ToJsonObject();
            JsonObject result = new JsonObject();
            result.AddArray("CustomLayers", jos);
            string json = result.ToJson();
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] CustomLayerHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            if (boundVariables["customLayersID"] == null)
            {
                JsonObject obj = new JsonObject();
                CustomLayerInfo[] layerInfos = GetCustomLayerInfos();
                JsonObject[] jos = new JsonObject[layerInfos.Length];
                for (int i = 0; i < layerInfos.Length; i++)
                    jos[i] = layerInfos[i].ToJsonObject();
                obj.AddArray("CustomLayers", jos);
                return Encoding.UTF8.GetBytes(obj.ToJson());
            }
            int layerID = Convert.ToInt32(boundVariables["customLayersID"]);
            CustomLayerInfo layerInfo = GetCustomLayerInfo(layerID);
            string json = layerInfo.ToJsonObject().ToJson();
            return Encoding.UTF8.GetBytes(json);
        }
        #endregion

        #region Operation Handlers
        private byte[] GetFeatureLayerExtentOperationHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int layerID = -1;
            if (!string.IsNullOrEmpty(boundVariables["customLayersID"]))
                layerID = Convert.ToInt32(boundVariables["customLayersID"]);
            else if (operationInput.TryGetAsLong("layerId", out long? lng))
                layerID = (int)lng;
            string extentString = "{{\"xmin\": {0},\"ymin\": {1},\"xmax\": {2},\"ymax\": {3},\"spatialReference\": {{\"wkid\": {4},\"latestWkid\": {5}}}}}";
            IFeatureClass featureClass = GetFeatureClass(layerID);
            if (featureClass != null && featureClass is IFeatureClassManage manage)
            {
                ISchemaLock schemaLock = (ISchemaLock)featureClass;
                try
                {
                    IFeatureClassManage featureClassManage = manage;
                    featureClassManage.UpdateExtent();
                    schemaLock.ChangeSchemaLock(esriSchemaLock.esriSharedSchemaLock);
                }
                catch (Exception e)
                {
                    schemaLock.ChangeSchemaLock(esriSchemaLock.esriSharedSchemaLock);
                    Console.WriteLine(e.Message);
                }
                if (featureClass is IGeoDataset geoDataset)
                    extentString = string.Format(extentString, geoDataset.Extent.XMin, geoDataset.Extent.YMin, geoDataset.Extent.XMax, geoDataset.Extent.YMax, geoDataset.SpatialReference.FactoryCode, geoDataset.SpatialReference.FactoryCode);
                else
                    extentString = "{}";
            }
            return Encoding.UTF8.GetBytes(extentString);
        }

        private byte[] FindNearestFeaturesOperationHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            IGeometry location = null;
            int distance = -1;
            if (operationInput.TryGetJsonObject("location", out JsonObject locationJson))
                location = Conversion.ToGeometry(locationJson, esriGeometryType.esriGeometryPoint);
            if (operationInput.TryGetAsLong("distance", out long? _distance))
                distance = (int)_distance;
            int layerID = -1;
            if (!string.IsNullOrEmpty(boundVariables["customLayersID"]))
                layerID = Convert.ToInt32(boundVariables["customLayersID"]);
            else if (operationInput.TryGetAsLong("layerId", out long? lng))
                layerID = (int)lng;
            string nearestFeatureItemTemplate = "{{\"id\":{0},\"objectid\":{1},\"x_lon\":{2},\"y_lat\":{3},\"distance\":{4}}}";
            string nearestFeature = "[]";
            IFeatureClass featureClass = GetFeatureClass(layerID);
            if (featureClass != null && featureClass is IFeatureClassManage manage)
            {
                if (featureClass is ISchemaLock schemaLock)
                {
                    try
                    {
                        IFeatureClassManage featureClassManage = manage;
                        featureClassManage.UpdateExtent();
                        schemaLock.ChangeSchemaLock(esriSchemaLock.esriSharedSchemaLock);
                    }
                    catch (Exception e)
                    {
                        schemaLock.ChangeSchemaLock(esriSchemaLock.esriSharedSchemaLock);
                        Console.WriteLine(e.Message);
                    }
                }
                if (featureClass != null && location != null)
                {
                    string subfields = null;
                    if (featureClass.Fields.FindField("ID") > 0)
                        subfields = "ID";
                    List<FindNearestFunctionality.NearItem> nears = FindNearestFunctionality.FindNearest(location, featureClass, distance, "", subfields);
                    if (nears.Count > 0)
                    {
                        string[] nearjsonArray = nears.Select(near => { return string.Format(nearestFeatureItemTemplate, near.id, near.oid, near.x, near.y, near.distance); }).ToArray();
                        nearestFeature = "[" + string.Join(",", nearjsonArray) + "]";
                    }
                }
            }
            return Encoding.UTF8.GetBytes(nearestFeature);
        }

        private byte[] GetTileImageOperationHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            RestResponseProperties props = new RestResponseProperties
            {
                ContentType = "image/png"
            };
            responseProperties = props.ToString();
            int level = -1, row = -1, column = -1;
            if (operationInput.TryGetAsLong("level", out long? _level))
                level = (int)_level;
            if (operationInput.TryGetAsLong("row", out long? _row))
                row = (int)_row;
            if (operationInput.TryGetAsLong("column", out long? _column))
                column = (int)_column;
            byte[] responseImage = null;
            if (useDynamicTile)
            {
                Utility.CalculateBBox(TilingScheme.TileOrigin, TilingScheme.LODs[level].Resolution, TilingScheme.TileRows, TilingScheme.TileCols, row, column, out double xmin, out double ymin, out double xmax, out double ymax);
                int tileRows = TilingScheme.TileRows;
                int tileCols = TilingScheme.TileCols;
                string format = TilingScheme.CacheTileFormat.ToString().ToUpper().Contains("PNG") ? TilingScheme.CacheTileFormat.ToString() : "jpg";
                StringBuilder queryString = new StringBuilder();
                queryString.Append("dpi=" + TilingScheme.DPI + "&");
                queryString.Append("transparent=true" + "&");
                queryString.Append("format=" + format + "&");
                queryString.AppendFormat("bbox={0}%2C{1}%2C{2}%2C{3}&", xmin.ToString(culture), ymin.ToString(culture), xmax.ToString(culture), ymax.ToString(culture));
                queryString.Append("bboxSR=" + TilingScheme.WKID + "&");
                queryString.Append("imageSR=" + TilingScheme.WKID + "&");
                queryString.Append("size=" + tileCols + "%2C" + tileRows + "&");
                queryString.Append("f=image");
                string uri = GetServiceRestUrlBase.Replace("https", "http") + "export?" + queryString;
                int tryCount = 0;
                while (tryCount < 3)
                {
                    try
                    {
                        HttpWebRequest lxRequest = (HttpWebRequest)WebRequest.Create(uri);
                        string lsResponse = string.Empty;
                        HttpWebResponse lxResponse = (HttpWebResponse)lxRequest.GetResponse();
                        MemoryStream ms = new MemoryStream();
                        lxResponse.GetResponseStream().CopyTo(ms);
                        responseImage = ms.ToArray();
                        break;
                    }
                    catch (Exception)
                    {
                        tryCount++;
                        Thread.Sleep(20);
                    }
                }
            }
            else
            {
                WebClient webClient = new WebClient();
                responseImage = webClient.DownloadData(GetServiceRestUrlBase + "tile/" + level + "/" + row + "/" + column);
            }
            return responseImage;
        }

        private byte[] GetLayoutDocumentsOperationHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            if (!operationInput.TryGetAsBoolean("returnGeneral", out bool? returnGeneralValue))
                returnGeneralValue = false;
            int layerID = -1;
            if (!string.IsNullOrEmpty(boundVariables["customLayersID"]))
                layerID = Convert.ToInt32(boundVariables["customLayersID"]);
            else if (operationInput.TryGetAsLong("layerId", out long? lng))
                layerID = (int)lng;
            List<LayoutDocument> layoutDocuments = GetLayoutDocuments(returnGeneralValue.Value, layerID);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(layoutDocuments));
        }

        private List<LayoutDocument> GetLayoutDocuments(bool returnGeneral, int layerID)
        {
            List<LayoutDocument> documents = new List<LayoutDocument>();
            if (Directory.Exists(GetServiceKrokiPath))
            {
                DirectoryInfo diRoot = new DirectoryInfo(GetServiceKrokiPath);
                if (returnGeneral)
                {
                    List<LayoutDocument> generalDocuments = GetLayoutDocuments(diRoot, "GenelKrokiler");
                    documents.AddRange(generalDocuments);
                }
                DirectoryInfo[] dis;
                if (layerID >= 0)
                {
                    dis = diRoot.GetDirectories(layerID.ToString());
                    foreach (DirectoryInfo di in dis)
                    {
                        List<LayoutDocument> layerDocuments = GetLayoutDocuments(di, layerID);
                        documents.AddRange(layerDocuments);
                    }
                }
                else
                {
                    dis = diRoot.GetDirectories();
                    foreach (DirectoryInfo di in dis)
                    {
                        if (int.TryParse(di.Name, out int folderLayer))
                        {
                            List<LayoutDocument> layerDocuments = GetLayoutDocuments(di, folderLayer);
                            documents.AddRange(layerDocuments);
                        }
                    }
                }
            }
            return documents;
        }

        private List<LayoutDocument> GetLayoutDocuments(DirectoryInfo subFolderInfo, int layerId)
        {
            List<LayoutDocument> documents = new List<LayoutDocument>();
            if (subFolderInfo != null)
            {
                FileInfo[] mxdFiles = subFolderInfo.GetFiles("*.mxd", SearchOption.TopDirectoryOnly);
                foreach (FileInfo mxdFileInfo in mxdFiles)
                {
                    LayoutDocument document = GetLayoutDocument(mxdFileInfo);
                    document.layerId = layerId;
                    if (document != null)
                        documents.Add(document);
                }
            }
            return documents;
        }

        private List<LayoutDocument> GetLayoutDocuments(DirectoryInfo rootFolderInfo, string subFolder)
        {
            List<LayoutDocument> documents = new List<LayoutDocument>();
            DirectoryInfo subKrokiFolderInfo = rootFolderInfo.GetDirectories(subFolder, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (subKrokiFolderInfo != null)
            {
                FileInfo[] mxdFiles = subKrokiFolderInfo.GetFiles("*.mxd", SearchOption.TopDirectoryOnly);
                foreach (FileInfo mxdFileInfo in mxdFiles)
                {
                    LayoutDocument document = GetLayoutDocument(mxdFileInfo);
                    document.layerId = -1;
                    if (document != null)
                        documents.Add(document);
                }
            }
            return documents;
        }

        private LayoutDocument GetLayoutDocument(FileInfo mxdFileInfo)
        {
            LayoutDocument document = new LayoutDocument();
            try
            {
                IMapDocument mapDocument = new MapDocumentClass();
                mapDocument.Open(mxdFileInfo.FullName);
                document.fullName = mxdFileInfo.FullName;
                document.name = mxdFileInfo.Name.Replace(".mxd", "");
                if (mapDocument.ActiveView is IPageLayout)
                    mapDocument.SetActiveView(mapDocument.ActiveView.FocusMap as IActiveView);
                IPageLayout pageLayout = mapDocument.PageLayout;
                document.elements = GetLayoutElements(pageLayout);
                mapDocument.Close();
                ReleaseCOMObject(mapDocument);
            }
            catch (Exception)
            {
                document = null;
            }
            return document;
        }

        private byte[] ExportLayoutOperationHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            KrokiRequest krokiRequest = ParseKrokiSettings(boundVariables, operationInput);
            byte[] result = ExportLayout(krokiRequest);
            return result;
        }

        private KrokiRequest ParseKrokiSettings(NameValueCollection boundVariables, JsonObject operationInput)
        {
            operationInput.TryGetString("krokiName", out string krokiName);
            operationInput.TryGetJsonObject("extent", out JsonObject extentValue);
            IEnvelope extent = Conversion.ToGeometry(extentValue, esriGeometryType.esriGeometryEnvelope) as IEnvelope;
            double scale = -1;
            if (operationInput.TryGetAsDouble("scale", out double? scaleValue))
                scale = scaleValue.Value;
            double expand = 1.0;
            if (operationInput.TryGetAsDouble("expand", out double? expandValue))
                expand = expandValue.Value;
            if (!operationInput.TryGetString("outputFileExtension", out string outputFileExtension))
                outputFileExtension = "PNG";
            KrokiSettings krokiSettings = new KrokiSettings();
            operationInput.TryGetJsonObject("settings", out JsonObject settings);
            if (settings != null)
            {
                try
                {
                    var o = JsonConvert.DeserializeObject<KrokiSettings>(settings.ToJson());
                    krokiSettings = o;
                }
                catch (Exception)
                {
                }
            }
            int layerID = -1;
            if (!string.IsNullOrEmpty(boundVariables["customLayersID"]))
                layerID = Convert.ToInt32(boundVariables["customLayersID"]);
            else if (operationInput.TryGetAsLong("layerId", out long? lng))
                layerID = (int)lng;
            int mapLayerIndex = 0;
            if (!string.IsNullOrEmpty(boundVariables["mapLayerIndex"]))
                mapLayerIndex = Convert.ToInt32(boundVariables["mapLayerIndex"]);
            else if (operationInput.TryGetAsLong("mapLayerIndex", out long? lng))
                mapLayerIndex = (int)lng;
            operationInput.TryGetString("filterFieldName", out string filterFieldName);
            string filterValues = null;
            if (operationInput.TryGetObject("filterValues", out object filterValuesObject))
                filterValues = filterValuesObject.ToString();
            operationInput.TryGetString("definitionExpression", out string definitionExpression);
            KrokiRequest krokiRequest = new KrokiRequest() { KrokiName = krokiName, Extent = extent, Expand = expand, Scale = scale, OutputFileExtension = GetContentTypeExtensionEnum(outputFileExtension), DefinitionExpression = definitionExpression, Settings = krokiSettings, LayerId = layerID, MapLayerIndex = mapLayerIndex, FilterFieldName = filterFieldName, FilterValues = filterValues };
            return krokiRequest;
        }

        private ContentTypeFileExtensionEnum GetContentTypeExtensionEnum(string outputFileExtension)
        {
            if (!Enum.TryParse(outputFileExtension, true, out ContentTypeFileExtensionEnum eu))
                eu = ContentTypeFileExtensionEnum.PNG;
            return eu;
        }
        #endregion

        #region Business Methods
        internal IFeatureClass GetFeatureClass(int layerId)
        {
            IFeatureClass m_fcToQuery;
            if (idBasedIFeatureClasses.ContainsKey(layerId))
                m_fcToQuery = idBasedIFeatureClasses[layerId];
            else
            {
                try
                {
                    IMapServer mapServer = (IMapServer)serverObjectHelper.ServerObject;
                    string mapName = mapServer.DefaultMapName;
                    IMapServerDataAccess dataAccess = (IMapServerDataAccess)mapServer;
                    m_fcToQuery = (IFeatureClass)dataAccess.GetDataSource(mapName, layerId);
                    idBasedIFeatureClasses.Add(layerId, m_fcToQuery);
                }
                catch (Exception)
                {
                    m_fcToQuery = null;
                }
            }
            return m_fcToQuery;
        }

        internal IFeatureClass GetFeatureClass(string layerName)
        {
            IFeatureClass m_fcToQuery = null;
            try
            {
                IMapServer mapServer = (IMapServer)serverObjectHelper.ServerObject;
                string mapName = mapServer.DefaultMapName;
                IMapLayerInfo layerInfo;
                IMapLayerInfos layerInfos = mapServer.GetServerInfo(mapName).MapLayerInfos;
                int c = layerInfos.Count;
                int layerIndex = 0;
                for (int i = 0; i < c; i++)
                {
                    layerInfo = layerInfos.get_Element(i);
                    if (layerInfo.Name == layerName)
                    {
                        layerIndex = layerInfo.ID;
                        break;
                    }
                }
                IMapServerDataAccess dataAccess = (IMapServerDataAccess)mapServer;
                m_fcToQuery = (IFeatureClass)dataAccess.GetDataSource(mapName, layerIndex);
            }
            catch
            {
            }
            return m_fcToQuery;
        }

        private CustomLayerInfo GetCustomLayerInfo(int layerID)
        {
            if (layerID < 0)
                throw new ArgumentOutOfRangeException("layerID");
            IMapLayerInfo layerInfo;
            long c = layerInfos.Count;
            for (int i = 0; i < c; i++)
            {
                layerInfo = layerInfos.get_Element(i);
                if (layerInfo.ID == layerID)
                    return new CustomLayerInfo(layerInfo);
            }
            throw new ArgumentOutOfRangeException("layerID");
        }

        private CustomLayerInfo[] GetCustomLayerInfos()
        {
            int c = layerInfos.Count;
            CustomLayerInfo[] customLayerInfos = new CustomLayerInfo[c];
            for (int i = 0; i < c; i++)
            {
                IMapLayerInfo layerInfo = layerInfos.get_Element(i);
                customLayerInfos[i] = new CustomLayerInfo(layerInfo);
            }
            return customLayerInfos;
        }

        protected TilingScheme ReadGoogleMapsTilingScheme(out TilingScheme tilingScheme)
        {
            tilingScheme = new TilingScheme();
            StringBuilder sb;
            #region Google/Bing maps tiling scheme
            tilingScheme.Path = "N/A";
            tilingScheme.CacheTileFormat = ImageFormat.PNG;
            tilingScheme.CompressionQuality = 75;
            tilingScheme.DPI = 96;
            tilingScheme.LODs = new LODInfo[20];
            const double cornerCoordinate = 20037508.342787;
            double resolution = cornerCoordinate * 2 / 256;
            double scale = 591657527.591555;
            for (int i = 0; i < tilingScheme.LODs.Length; i++)
            {
                tilingScheme.LODs[i] = new LODInfo()
                {
                    Resolution = resolution,
                    LevelID = i,
                    Scale = scale
                };
                resolution /= 2;
                scale /= 2;
            }
            sb = new StringBuilder("\r\n");
            foreach (LODInfo lod in tilingScheme.LODs)
                sb.Append(@"      {""level"":" + lod.LevelID + "," + @"""resolution"":" + lod.Resolution + "," + @"""scale"":" + lod.Scale + @"}," + "\r\n");
            tilingScheme.LODsJson = sb.ToString().Remove(sb.ToString().Length - 3);
            tilingScheme.InitialExtent = new EnvelopeClass();
            tilingScheme.InitialExtent.PutCoords(-cornerCoordinate, -cornerCoordinate, cornerCoordinate, cornerCoordinate);
            tilingScheme.FullExtent = tilingScheme.InitialExtent;
            tilingScheme.PacketSize = 0;
            tilingScheme.StorageFormat = StorageFormat.esriMapCacheStorageModeExploded;
            tilingScheme.TileCols = tilingScheme.TileRows = 256;
            tilingScheme.TileOrigin = new Point();
            tilingScheme.TileOrigin.PutCoords(-cornerCoordinate, cornerCoordinate);
            tilingScheme.WKID = 3857;
            tilingScheme.WKT = @"PROJCS[""WGS_1984_Web_Mercator_Auxiliary_Sphere"",GEOGCS[""GCS_WGS_1984"",DATUM[""D_WGS_1984"",SPHEROID[""WGS_1984"",6378137.0,298.257223563]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]],PROJECTION[""Mercator_Auxiliary_Sphere""],PARAMETER[""False_Easting"",0.0],PARAMETER[""False_Northing"",0.0],PARAMETER[""Central_Meridian"",0.0],PARAMETER[""Standard_Parallel_1"",0.0],PARAMETER[""Auxiliary_Sphere_Type"",0.0],UNIT[""Meter"",1.0],AUTHORITY[""ESRI"",""3857""]]";
            #endregion
            return tilingScheme;
        }

        private byte[] ExportLayout(KrokiRequest request)
        {
            string outputFilePath = "";
            string fileName = null;
            bool isGeneralLayout = request.LayerId == -1;
            bool hasFeature = false;
            string rootLayoutFolderPath = (isGeneralLayout) ? "GenelKrokiler" : request.LayerId.ToString();
            string mxdFilePath = GetMxdPath(rootLayoutFolderPath, request.KrokiName, request.OutputFileExtension.ToString().ToLower(), out bool hasError);
            if (hasError)
                outputFilePath = GetServiceRestOutputUrlBase + "/" + mxdFilePath;
            else
            {
                IMapDocument mapDocument = new MapDocumentClass();
                mapDocument.Open(mxdFilePath);
                if (mapDocument.ActiveView is IPageLayout)
                    mapDocument.SetActiveView(mapDocument.ActiveView.FocusMap as IActiveView);
                IActiveView activeView = mapDocument.ActiveView;
                IPageLayout pageLayout = mapDocument.PageLayout;
                mapDocument.Close();
                ReleaseCOMObject(mapDocument);
                SetElementValues(pageLayout, request.Settings);
                IActiveView pageLayoutActiveView = pageLayout as IActiveView;
                IMapAutoExtentOptions mapAutoExtentOptions = (IMapAutoExtentOptions)activeView.FocusMap;
                if (isGeneralLayout)
                {
                    if (request.Extent != null)
                    {
                        if (request.Extent.SpatialReference == null)
                            request.Extent.SpatialReference = activeView.FocusMap.SpatialReference;
                        else if (request.Extent.SpatialReference.FactoryCode != activeView.FocusMap.SpatialReference.FactoryCode)
                            request.Extent.Project(activeView.FocusMap.SpatialReference);
                        activeView.Extent = request.Extent;
                    }
                }
                else
                {
                    ILayer layer = pageLayoutActiveView.FocusMap.get_Layer(request.MapLayerIndex);
                    IFeatureLayer featureLayer = layer as IFeatureLayer;
                    IFeatureClass featureClass = featureLayer.FeatureClass;
                    if (featureClass == null)
                        featureClass = GetFeatureClass(request.LayerId);
                    IFeatureLayerDefinition featureLayerDefinition = featureLayer as IFeatureLayerDefinition;
                    string queryOIDFieldName = featureClass.OIDFieldName;
                    if (!string.IsNullOrEmpty(request.FilterValues))
                    {
                        if (string.IsNullOrEmpty(request.FilterFieldName))
                            featureLayerDefinition.DefinitionExpression = request.FilterValues;
                        else
                        {
                            string seperator = "";
                            if (layer is IDisplayRelationshipClass displayRelationshipClass && request.FilterFieldName.Contains("."))
                            {
                                IObjectClass origin = displayRelationshipClass.RelationshipClass.OriginClass;
                                IObjectClass destination = displayRelationshipClass.RelationshipClass.DestinationClass;
                                string[] splitFieldName = request.FilterFieldName.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                                string userName = "";
                                string objectClassName = "";
                                string fieldName = "";
                                for (int i = 0; i < splitFieldName.Length; i++)
                                {
                                    int index = splitFieldName.Length - i - 1;
                                    if (index == 0)
                                        fieldName = splitFieldName[i];
                                    else if (index == 1)
                                        objectClassName = splitFieldName[i];
                                    else if (index == 2)
                                        userName = splitFieldName[i];
                                }
                                queryOIDFieldName = objectClassName + "." + fieldName;
                                if (!string.IsNullOrEmpty(userName))
                                    queryOIDFieldName = userName + "." + queryOIDFieldName;
                                if (objectClassName.ToLower(new CultureInfo("tr-TR")) == origin.AliasName.ToLower(new CultureInfo("tr-TR")))
                                {
                                    int fieldIndex = origin.FindField(fieldName);
                                    if (fieldIndex > -1)
                                    {
                                        IField field = origin.Fields.get_Field(fieldIndex);
                                        seperator = GetFieldSeperator(field.Type);
                                    }
                                }
                                else
                                {
                                    int fieldIndex = destination.FindField(fieldName);
                                    if (fieldIndex > -1)
                                    {
                                        IField field = destination.Fields.get_Field(fieldIndex);
                                        seperator = GetFieldSeperator(field.Type);
                                    }
                                }
                            }
                            else
                            {
                                int fieldIndex = featureClass.FindField(request.FilterFieldName);
                                if (fieldIndex > -1)
                                {
                                    IField field = featureClass.Fields.get_Field(fieldIndex);
                                    seperator = GetFieldSeperator(field.Type);
                                }
                            }
                            string[] splits = request.FilterValues.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            featureLayerDefinition.DefinitionExpression = request.FilterFieldName + " IN (" + string.Join(",", splits.ToList().Select(a => seperator + a.Trim() + seperator)) + ")";
                            featureLayerDefinition.DefinitionExpression = featureLayerDefinition.DefinitionExpression.Replace("''", "'");
                        }
                    }
                    if (!string.IsNullOrEmpty(request.DefinitionExpression))
                    {
                        if (string.IsNullOrEmpty(featureLayerDefinition.DefinitionExpression))
                            featureLayerDefinition.DefinitionExpression = request.DefinitionExpression;
                        else
                            featureLayerDefinition.DefinitionExpression += " AND (" + request.DefinitionExpression + ")";
                    }
                    if (string.IsNullOrEmpty(featureLayerDefinition.DefinitionExpression))
                        featureLayerDefinition.DefinitionExpression = "1=2";
                    SetDefinitionExpressions(pageLayoutActiveView.FocusMap, featureLayerDefinition.DefinitionExpression);
                    if (mapAutoExtentOptions.AutoExtentType == esriExtentTypeEnum.esriAutoExtentNone && featureLayerDefinition.DefinitionExpression != "1=2")
                    {
                        IQueryFilter qfSelection = new QueryFilterClass
                        {
                            WhereClause = featureLayerDefinition.DefinitionExpression
                        };
                        IFeatureSelection featureSelection = featureLayer as IFeatureSelection;
                        featureSelection.SelectFeatures(qfSelection, esriSelectionResultEnum.esriSelectionResultNew, false);
                        hasFeature = featureSelection.SelectionSet.Count > 0;
                        if (featureSelection.SelectionSet.Count > 0)
                        {
                            IEnumIDs enumIDs = featureSelection.SelectionSet.IDs;
                            int j = -1;
                            Dictionary<int, List<int>> s = new Dictionary<int, List<int>>();
                            for (int i = 0; i < featureSelection.SelectionSet.Count; i++)
                            {
                                if (i % 1000 == 0)
                                {
                                    j++;
                                    s.Add(j, new List<int>());
                                }
                                s[j].Add(enumIDs.Next());
                            }
                            List<string> queries = new List<string>();
                            foreach (var item in s)
                            {
                                string queryWhere = queryOIDFieldName + " IN (";
                                queryWhere += string.Join(",", item.Value.ToArray());
                                queryWhere += ")";
                                queries.Add(queryWhere);
                            }
                            featureSelection.Clear();
                            IQueryFilter qfQuery = new QueryFilterClass
                            {
                                WhereClause = string.Join(" OR ", queries)
                            };
                            IFeatureCursor fCursor = featureClass.Search(qfQuery, false);
                            IGeometryBag geometryBag = new GeometryBagClass();
                            IGeometryCollection geometryCollection = (IGeometryCollection)geometryBag;
                            IFeature feature = null;
                            object missingType = Type.Missing;
                            while ((feature = fCursor.NextFeature()) != null)
                            {
                                if (feature.Shape.GeometryType == esriGeometryType.esriGeometryPoint || feature.Shape.GeometryType == esriGeometryType.esriGeometryMultipoint)
                                {
                                    ITopologicalOperator topoOp = feature.Shape as ITopologicalOperator;
                                    IGeometry bufferGeometry = topoOp.Buffer(0.0001);
                                    geometryCollection.AddGeometry(bufferGeometry, ref missingType, ref missingType);
                                }
                                else
                                    geometryCollection.AddGeometry(feature.Shape, ref missingType, ref missingType);
                            }
                            Marshal.ReleaseComObject(fCursor);
                            if (geometryCollection.GeometryCount > 0)
                            {
                                activeView.Extent = geometryBag.Envelope;
                                MapProperties mapProperties = GetMapProperties(activeView.FocusMap.Description);
                                IMapClipOptions mapClilpOptions = (IMapClipOptions)activeView.FocusMap;
                                if (mapProperties.ClipEnabled)
                                {
                                    mapClilpOptions.ClipType = esriMapClipType.esriMapClipShape;
                                    ITopologicalOperator unionedPolygon = new PolygonClass();
                                    unionedPolygon.ConstructUnion(geometryBag as IEnumGeometry);
                                    IGeometry clipGeometry = unionedPolygon as IPolygon;
                                    if (mapProperties.BufferDistance > 0)
                                        clipGeometry = unionedPolygon.Buffer(mapProperties.BufferDistance);
                                    mapClilpOptions.ClipGeometry = clipGeometry;
                                    if (mapProperties.ClipExcludeLayers.Count > 0)
                                    {
                                        mapClilpOptions.ClipFilter.RemoveAll();
                                        ISet clipFilter = new SetClass();
                                        foreach (int excludeLayerId in mapProperties.ClipExcludeLayers)
                                        {
                                            if (excludeLayerId < activeView.FocusMap.LayerCount)
                                            {
                                                ILayer excludeLayer = activeView.FocusMap.get_Layer(excludeLayerId);
                                                clipFilter.Add(excludeLayer);
                                            }
                                        }
                                        mapClilpOptions.ClipFilter = clipFilter;
                                    }
                                }
                            }
                        }
                    }
                }
                if (request.Scale > 0)
                {
                    pageLayoutActiveView.Activate(GetDesktopWindow());
                    activeView.ScreenDisplay.DisplayTransformation.ScaleRatio = request.Scale;
                }
                else if (request.Expand != 1.0)
                {
                    IEnvelope envelope = activeView.Extent;
                    envelope.Expand(request.Expand, request.Expand, true);
                    activeView.Extent = envelope;
                }
                activeView.Refresh();
                pageLayoutActiveView.Refresh();

                fileName = ExportActiveViewParameterized(pageLayoutActiveView, 300, 1, request.OutputFileExtension, GetPhysicalRestOutputFolder, false);
                outputFilePath = GetServiceRestOutputUrlBase + "/" + fileName;
                outputFilePath = outputFilePath.ToLower(culture);
            }
            byte[] retValue = Encoding.UTF8.GetBytes("{\"outputFilePath\":\"" + outputFilePath + "\"}");
            return retValue;
        }

        private MapProperties GetMapProperties(string mapDescription)
        {
            MapProperties mp = new MapProperties();
            if (false == string.IsNullOrEmpty(mapDescription))
            {
                int start = mapDescription.IndexOf("<%");
                int end = mapDescription.LastIndexOf("%>");
                if (start > -1 && end > start)
                {
                    string mpt = mapDescription.Substring(start + 2, end - start - 2);
                    mp = JsonConvert.DeserializeObject<MapProperties>(mpt);
                }
            }
            return mp;
        }

        private void SetDefinitionExpressions(IMap map, string expression)
        {
            IEnumLayer enumLayer = map.get_Layers(null, true);
            enumLayer.Reset();
            ILayer layer;
            while ((layer = enumLayer.Next()) != null)
            {
                if (layer is IFeatureLayer featureLayer)
                    if (featureLayer is IFeatureLayerDefinition featureLayerDefinition)
                        if (!string.IsNullOrEmpty(featureLayerDefinition.DefinitionExpression) && featureLayerDefinition.DefinitionExpression.Contains("#definitionexpression#"))
                            featureLayerDefinition.DefinitionExpression = featureLayerDefinition.DefinitionExpression.Replace("#definitionexpression#", " " + expression + " ");
            }
        }
        public static void ReleaseCOMObject(object o)
        {
            if ((o != null) && Marshal.IsComObject(o))
            {
                while (Marshal.ReleaseComObject(o) > 0)
                {
                }
            }
        }
        private static string GetFieldSeperator(esriFieldType esriFieldType)
        {
            string seperator = "";
            switch (esriFieldType)
            {
                case esriFieldType.esriFieldTypeBlob:
                    break;
                case esriFieldType.esriFieldTypeDate:
                case esriFieldType.esriFieldTypeGUID:
                case esriFieldType.esriFieldTypeGlobalID:
                case esriFieldType.esriFieldTypeString:
                case esriFieldType.esriFieldTypeXML:
                    seperator = "'";
                    break;
                default:
                    break;
            }
            return seperator;
        }

        [DllImport("User32.dll")]
        public static extern int GetDesktopWindow();
        private void SetElementValues(IPageLayout pageLayout, KrokiSettings krokiSettings)
        {
            IGraphicsContainer gc = pageLayout as IGraphicsContainer;
            gc.Reset();
            IElement element = null;
            while ((element = gc.Next()) != null)
            {
                IElementProperties ep = element as IElementProperties;
                string elementName = "";
                if (krokiSettings.Elements != null && !string.IsNullOrEmpty(elementName = ep.Name))
                {
                    LayoutElement ev = krokiSettings.Elements.FirstOrDefault(a => a.Name.Split('#').Last().Equals(elementName.Split('#').Last(), StringComparison.InvariantCultureIgnoreCase));
                    if (ev != null && element is ITextElement textElement)
                        textElement.Text = ev.Value;
                }
            }
        }

        private List<LayoutElement> GetLayoutElements(IPageLayout pageLayout)
        {
            List<LayoutElement> elements = new List<LayoutElement>();
            IGraphicsContainer gc = pageLayout as IGraphicsContainer;
            gc.Reset();
            IElement element;
            while ((element = gc.Next()) != null)
            {
                IElementProperties ep = element as IElementProperties;
                if (false == string.IsNullOrEmpty(ep.Name))
                {
                    if (element is ITextElement)//TODO use other elements to change content
                    {
                        ITextElement textElement = element as ITextElement;
                        elements.Add(new LayoutElement() { Name = ep.Name, Value = textElement.Text, Type = ep.Type });
                    }
                }
            }
            return elements;
        }

        private string GetMxdPath(string categoryFolder, string layoutFolder, string outputExtension, out bool hasError)
        {
            hasError = false;
            string layerKrokiFolderPath = System.IO.Path.Combine(GetServiceKrokiPath, categoryFolder);
            string mxdFilePath;
            if (!string.IsNullOrEmpty(layoutFolder))
                mxdFilePath = System.IO.Path.Combine(layerKrokiFolderPath, layoutFolder + ".mxd");
            else
                mxdFilePath = System.IO.Path.Combine(layerKrokiFolderPath, "default.mxd");
            if (!File.Exists(mxdFilePath))
            {
                string errorPagesDirectory = System.IO.Path.Combine(GetServiceKrokiPath, @"Files\KrokiErrors");
                hasError = true;
                string errorFileName = "hata." + outputExtension;
                string copyFilePath = System.IO.Path.Combine(errorPagesDirectory, errorFileName);
                string outputFilePath = System.IO.Path.Combine(GetPhysicalRestOutputFolder, "_ags_KrokiError-" + errorFileName);
                if (!File.Exists(outputFilePath))
                    File.Copy(copyFilePath, outputFilePath);
                mxdFilePath = "_ags_KrokiError-" + errorFileName;
            }
            return mxdFilePath;
        }

        private string ExportActiveViewParameterized(IActiveView docActiveView, long iOutputResolution, long lResampleRatio, ContentTypeFileExtensionEnum ExportType, string sOutputDir, bool bClipToGraphicsExtent)
        {

            /* EXPORT PARAMETER: (iOutputResolution) the resolution requested.
             * EXPORT PARAMETER: (lResampleRatio) Output Image Quality of the export.  The value here will only be used if the export
             * object is a format that allows setting of Output Image Quality, i.e. a vector exporter.
             * The value assigned to ResampleRatio should be in the range 1 to 5.
             * 1 corresponds to "Best", 5 corresponds to "Fast"
             * EXPORT PARAMETER: (ExportType) a string which contains the export type to create.
             * EXPORT PARAMETER: (sOutputDir) a string which contains the directory to output to.
             * EXPORT PARAMETER: (bClipToGraphicsExtent) Assign True or False to determine if export image will be clipped to the graphic 
             * extent of layout elements.  This value is ignored for data view exports
             */

            /* Exports the Active View of the document to selected output format. */

            string exportFileName = "_ags_Kroki-" + Guid.NewGuid().ToString().Replace("-", "");
            // using predefined static member
            try
            {

                IExport docExport = null;
                IPrintAndExport docPrintExport;
                IOutputRasterSettings RasterSettings;

                // The Export*Class() type initializes a new export class of the desired type.
                switch (ExportType)
                {
                    case ContentTypeFileExtensionEnum.AI:
                        docExport = new ExportAIClass();
                        break;
                    case ContentTypeFileExtensionEnum.BMP:
                        docExport = new ExportBMPClass();
                        break;
                    case ContentTypeFileExtensionEnum.EMF:
                        docExport = new ExportEMFClass();
                        break;
                    case ContentTypeFileExtensionEnum.EPS:
                        docExport = new ExportPSClass();
                        break;
                    case ContentTypeFileExtensionEnum.GIF:
                        docExport = new ExportGIFClass();
                        break;
                    case ContentTypeFileExtensionEnum.JPG:
                        docExport = new ExportJPEGClass();
                        break;
                    case ContentTypeFileExtensionEnum.PDF:
                        docExport = new ExportPDFClass();
                        break;
                    case ContentTypeFileExtensionEnum.PNG:
                        docExport = new ExportPNGClass();
                        break;
                    case ContentTypeFileExtensionEnum.SVG:
                        docExport = new ExportSVGClass();
                        break;
                    case ContentTypeFileExtensionEnum.TIF:
                        //http://edndoc.esri.com/arcobjects/9.2/NET/ViewCodePages/244A41B7-9AE6-4ff3-ACA2-7D4E149F9E5FesriCstmExportGeoTiffExportCS_NetExportGeoTIFF.cs.htm
                        docExport = new ExportTIFFClass();
                        break;
                    default:
                        break;
                }

                docPrintExport = new PrintAndExportClass();

                //set the export filename (which is the nameroot + the appropriate file extension)
                exportFileName += "." + docExport.Filter.Split('.')[1].Split('|')[0].Split(')')[0];
                string exportFilePath = System.IO.Path.Combine(sOutputDir, exportFileName);
                docExport.ExportFileName = exportFilePath;

                //Output Image Quality of the export.  The value here will only be used if the export
                // object is a format that allows setting of Output Image Quality, i.e. a vector exporter.
                // The value assigned to ResampleRatio should be in the range 1 to 5.
                // 1 corresponds to "Best", 5 corresponds to "Fast"

                // check if export is vector or raster
                if (docExport is IOutputRasterSettings settings)
                {
                    // for vector formats, assign the desired ResampleRatio to control drawing of raster layers at export time   
                    RasterSettings = settings;
                    RasterSettings.ResampleRatio = (int)lResampleRatio;

                    // NOTE: for raster formats output quality of the DISPLAY is set to 1 for image export 
                    // formats by default which is what should be used
                }

                docPrintExport.Export(docActiveView, docExport, iOutputResolution, bClipToGraphicsExtent, null);
                ReleaseCOMObject(docPrintExport);
            }
            catch (COMException ex)
            {
                throw ex;
            }
            return exportFileName;
        }
        #endregion

        private Dictionary<string, string> GetEnvironmentProperties()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            IPropertySet b = ServerEnvironment.Properties;
            b.GetAllProperties(out object names, out object values);
            object[] nameList = (object[])names;
            object[] valueList = (object[])values;
            for (int i = 0; i < nameList.Length; i++)
                props.Add(nameList[i].ToString(), valueList[i] != null ? valueList[i].ToString() : string.Empty);
            return props;
        }

        public IServerEnvironment2 ServerEnvironment
        {
            get
            {
                if (serverEnvironment == null)
                {
                    UID uid = new UIDClass
                    {
                        Value = "{32D4C328-E473-4615-922C-63C108F55E60}"
                    };
                    IEnvironmentManager environmentManager = new EnvironmentManager();
                    serverEnvironment = environmentManager.GetEnvironment(uid) as IServerEnvironment2;
                }
                return serverEnvironment;
            }
        }
    }
}
