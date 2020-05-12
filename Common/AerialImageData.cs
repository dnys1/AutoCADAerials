using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Defines the data which describes the properties of a Google Maps image.
    /// </summary>
    public class AerialImageData
    {
        public GeoPoint Center;
        public GeoPoint NECorner;
        public GeoPoint SWCorner;
        public int Width;
        public int Height;
        public int Zoom;
        public MapType MapType;
        public string FileName;

        public AerialImageData()
        {

        }

        public AerialImageData(GeoPoint center, GeoPoint neCorner, GeoPoint swCorner, int width, int height, int zoom, MapType mapType, string fileName)
        {
            Center = center;
            NECorner = neCorner;
            SWCorner = swCorner;
            Width = width;
            Height = height;
            Zoom = zoom;
            MapType = mapType;
            FileName = fileName;
        }
    }
}
