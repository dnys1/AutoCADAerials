using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class GoogleMapsAerialRepository : AerialRepository
    {
        private static readonly string API_KEY = "AIzaSyDcQifInuUX8-7I_aqawkjQ2fzEpE32oyE";
        private static readonly string BaseGMapsUrl = "https://maps.googleapis.com/maps/api/staticmap?";

        public GoogleMapsAerialRepository()
        {
            MAX_HEIGHT = 640;
            MAX_WIDTH = 640;
        }

        public override string BuildGeocodeRequestUrl(string address)
        {
            throw new NotImplementedException();
        }

        public override string BuildImageRequestUrl(AerialImageData imageData, bool update = false)
        {
            // Get the format(jpeg / png)
            string format = GetFormatForMapType(imageData.MapType);

            // Scale is 2 no matter what. Always scale down width/height 
            // to allow API to scale back up.
            int scale = 2;

            // If updating, reduce image width/height to original
            int width = imageData.Width / (update ? 2 : 1);
            int height = imageData.Height / (update ? 2 : 1);

            string mapType;
            switch (imageData.MapType)
            {
                case MapType.Aerial:
                    mapType = "satellite";
                    break;
                case MapType.AerialWithLabels:
                    mapType = "hybrid";
                    break;
                default:
                    mapType = "roadmap";
                    break;
            }

            return BaseGMapsUrl + $"center={imageData.Center.lat},{imageData.Center.lng}&zoom={imageData.Zoom}&size={width}x{height}" +
                $"&format={format}&maptype={mapType}&scale={scale}&key={API_KEY}";
        }
    }
}
