using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{

    public class BingAerialRepository : AerialRepository
    {
        private static readonly string API_KEY = "AqFvYUfBX8kWS93b3VngB7bOK1Bl5fIZqaDvFM3-zFPvhDL8Rpk0GA5o7VTMxQxw";
        private static readonly string BaseBingMapsUrl = "https://dev.virtualearth.net/REST/v1/Imagery/Map/";

        public BingAerialRepository()
        {
            MAX_HEIGHT = 1500;
            MAX_WIDTH = 2000;
        }

        public override string BuildGeocodeRequestUrl(string address)
        {
            return "http://dev.virtualearth.net/REST/v1/Locations/" + address + "?o=xml&key=" + API_KEY;
        }

        /// <summary>
        /// Builds the Bing Maps Static API request for an image.
        /// </summary>
        /// <param name="imageData">The image's data.</param>
        /// <returns></returns>
        public override string BuildImageRequestUrl(AerialImageData imageData, bool update = false)
        {
            return BaseBingMapsUrl + $"{imageData.MapType}?" +
                $"mapArea={imageData.SWCorner.lat},{imageData.SWCorner.lng},{imageData.NECorner.lat},{imageData.NECorner.lng}" +
                $"&mapSize={imageData.Width},{imageData.Height}&key={API_KEY}";
        }
    }
}
