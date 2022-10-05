using ESRI.ArcGIS.Geometry;

namespace OrbisKroki.Classes
{
    public static class Utility
    {
        public static void CalculateBBox(Point tileOrigin, double resolution, int tileRows, int tileCols, int row, int col, out double xmin, out double ymin, out double xmax, out double ymax)
        {
            xmin = tileOrigin.X + resolution * tileCols * col;
            ymin = tileOrigin.Y - resolution * tileRows * (row + 1);
            xmax = tileOrigin.X + resolution * tileCols * (col + 1);
            ymax = tileOrigin.Y - resolution * tileRows * row;
        }
    }
}
