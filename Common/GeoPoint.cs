using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class GeoPoint
    {
        public double lat;
        public double lng;

        public GeoPoint(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
        }

        /// <summary>
        /// Returns the horizontal distance (in feet) to another GeoPoint.
        /// </summary>
        /// <param name="other">The other GeoPoint</param>
        /// <returns></returns>
        //public double HorizontalDistanceTo(GeoPoint other)
        //{
        //    double lat1 = Calculator.DegreesToRadians(lat);
        //    double lng1 = Calculator.DegreesToRadians(lng);
        //    double lat2 = Calculator.DegreesToRadians(other.lat);
        //    double lng2 = Calculator.DegreesToRadians(other.lng);

        //    return Math.Acos(Math.Sin(lat1))
        //}
    }
}
