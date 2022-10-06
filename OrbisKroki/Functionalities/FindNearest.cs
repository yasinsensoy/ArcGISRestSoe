using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace OrbisKroki.Functionalities
{
    public class FindNearestFunctionality
    {
        public class NearItem
        {
            public int id;
            public int oid;
            public double distance;
            public double x;
            public double y;
        }

        public static KeyValuePair<int, double> FindNearest(IGeometry g, IFeatureClass fc, string where = "")
        {
            IFeatureIndex fi = new FeatureIndexClass
            {
                FeatureClass = fc
            };
            if (!string.IsNullOrEmpty(where))
            {
                IQueryFilter qfilter = new QueryFilterClass
                {
                    WhereClause = where
                };
                IFeatureCursor fcursor = fc.Search(qfilter, true);
                fi.FeatureCursor = fcursor;
            }
            fi.Index(null, ((IGeoDataset)fc).Extent);
            ((IIndexQuery)fi).NearestFeature(g, out int fid, out double distance);
            return new KeyValuePair<int, double>(fid, distance);
        }

        /// <summary>
        /// This does a spatial query within a search radius, then loops
        /// over features in that area, checking distance between the source geometry and each feature. 
        /// At least the spatial query can use the spatial index. It is surprisingly fast.
        /// This will optionally honor any pre-existing selection in the query FeatureLayer - 
        /// that is, it can do a 'select from' the existing selection, if any.
        /// </summary>
        public static List<NearItem> FindNearest(IGeometry g, IFeatureClass flayer, double searchradius, string where = "", string subfields = null, bool honorselection = false)
        {
            IFeatureCursor fcursor = DoSpatialQuery(flayer, g, searchradius, where, subfields, honorselection);
            List<NearItem> nears = new List<NearItem>();
            nears = FindNearest(g, fcursor, searchradius > 0.0);
            Marshal.FinalReleaseComObject(fcursor);
            return nears;
        }

        ///<summary>
        /// Creates a spatial query which performs a spatial search for 
        /// features in the supplied feature class and has the option to also apply an attribute query via a where clause.
        /// See http://edndoc.esri.com/arcobjects/9.2/NET/7b4b8987-a3f0-4954-980f-720e61965449.htm
        /// By accepting an IFeatureLayer (instead of IFeatureClass) any definition query on the layer will be honored, and we have the option to honor any preexisting selection set.
        /// </summary>
        private static IFeatureCursor DoSpatialQuery(IFeatureClass flayer, IGeometry searchGeometry, double bufferdistance = 0.0, string where = "", string subfields = null, bool honorselection = false, esriSpatialRelEnum spatialRelation = esriSpatialRelEnum.esriSpatialRelIntersects, bool recycling = true)
        {
            ISpatialFilter spatialFilter = CreateSpatialFilter(flayer, searchGeometry, spatialRelation, bufferdistance, where, subfields);
            IFeatureCursor featureCursor = null;
            if (!honorselection)
                featureCursor = flayer.Search(spatialFilter, recycling);
            else
            {
                if (((IFeatureSelection)flayer).SelectionSet.Count > 0)
                {
                    ((IFeatureSelection)flayer).SelectionSet.Search(spatialFilter, recycling, out ICursor cursor);
                    featureCursor = (IFeatureCursor)cursor;
                }
                else
                    featureCursor = flayer.Search(spatialFilter, recycling);
            }
            Marshal.FinalReleaseComObject(spatialFilter);
            return featureCursor;
        }

        /// <summary>
        /// Loops over rows in a FeatureCursor, measuring the distance from 
        /// the target Geometry to each feature in the FeatureCursor, to
        /// find the closest one.
        /// </summary>
        private static List<NearItem> FindNearest(IGeometry g, IFeatureCursor fcursor, bool returnAll)
        {
            int closestoid = -1;
            double closestdistance = 0.0;
            IFeature nearestFeature = null;
            List<NearItem> nears = new List<NearItem>();
            int nearestID = -1;
            IFeature feature;
            while ((feature = fcursor.NextFeature()) != null)
            {
                if (feature.Shape.IsEmpty)
                    continue;
                double distance = ((IProximityOperator)g).ReturnDistance(feature.Shape);
                if (returnAll)
                {
                    int idIndex = feature.Fields.FindField("ID");
                    int ID = -1;
                    if (idIndex > -1)
                    {
                        object idObject = feature.get_Value(idIndex);
                        if (idObject != null)
                            ID = Convert.ToInt32(feature.get_Value(idIndex));
                    }
                    double X = ((IArea)feature.Shape).Centroid.X;
                    double Y = ((IArea)feature.Shape).Centroid.Y;
                    NearItem ni = new NearItem() { id = ID, oid = feature.OID, distance = Math.Round(distance, 2, MidpointRounding.AwayFromZero), x = Math.Round(X, 5, MidpointRounding.AwayFromZero), y = Math.Round(Y, 5, MidpointRounding.AwayFromZero) };
                    nears.Add(ni);
                }
                else if (closestoid == -1 || distance < closestdistance)
                {
                    int idIndex = feature.Fields.FindField("ID");
                    nearestID = -1;
                    if (idIndex > -1)
                    {
                        object idObject = feature.get_Value(idIndex);
                        if (idObject != null)
                            nearestID = Convert.ToInt32(idObject);
                    }
                    nearestFeature = feature;
                    closestoid = feature.OID;
                    closestdistance = distance;
                }
            }
            if (nearestFeature != null)
            {
                double X = ((IArea)nearestFeature.Shape).Centroid.X;
                double Y = ((IArea)nearestFeature.Shape).Centroid.Y;
                NearItem ni = new NearItem() { id = nearestID, oid = closestoid, distance = Math.Round(closestdistance, 2, MidpointRounding.AwayFromZero), x = Math.Round(X, 5, MidpointRounding.AwayFromZero), y = Math.Round(Y, 5, MidpointRounding.AwayFromZero) };
                nears.Add(ni);
            }
            return nears;
        }

        /// <summary>
        /// Utility function to create a spatial filter.
        /// </summary>
        private static ISpatialFilter CreateSpatialFilter(IFeatureClass flayer, IGeometry searchGeometry, esriSpatialRelEnum spatialRelation, double bufferdistance, string where, string subfields = null)
        {
            ISpatialFilter spatialFilter = new SpatialFilterClass
            {
                GeometryField = flayer.ShapeFieldName,
                SpatialRel = spatialRelation
            };
            if (bufferdistance > 0.0)
            {
                ISpatialReferenceFactory2 srf = new SpatialReferenceEnvironmentClass();
                searchGeometry.Project(srf.CreateSpatialReference(102100));
                ITopologicalOperator topoOperator = (ITopologicalOperator)searchGeometry;
                IGeometry buffer = topoOperator.Buffer(bufferdistance * 1000);
                searchGeometry.Project(srf.CreateSpatialReference(4326));
                buffer.Project(srf.CreateSpatialReference(4326));
                spatialFilter.Geometry = buffer;
            }
            if (string.IsNullOrEmpty(subfields))
                spatialFilter.SubFields = string.Format("{0},{1}", flayer.OIDFieldName, flayer.ShapeFieldName);
            else
                spatialFilter.SubFields = string.Format("{0},{1},{2}", flayer.OIDFieldName, flayer.ShapeFieldName, subfields);
            if (!string.IsNullOrEmpty(where))
                spatialFilter.WhereClause = where;
            return spatialFilter;
        }
    }
}
