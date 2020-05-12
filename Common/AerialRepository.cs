using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public delegate void ImageSavedCallback(MapSource mapSource, AerialImageData imageData);

    public abstract class AerialRepository
    {
        protected int MAX_HEIGHT;
        protected int MAX_WIDTH;

        public static void DownloadImage(string requestUrl, string fileName)
        {
            Debug.Assert(requestUrl != null, "Request URL cannot be null.");
            Debug.Assert(fileName != null, "Filename cannot be null.");

            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFile(requestUrl, fileName);
            }
        }

        /// <summary>
        /// Returns the format (PNG/JPEG) for the specified mapType.
        /// </summary>
        /// <param name="mapType">The type of map (Aerial, AerialWithLabels, Road).</param>
        /// <returns></returns>
        public static string GetFormatForMapType(MapType mapType)
        {
            string format;
            switch (mapType)
            {
                case MapType.Aerial:
                case MapType.AerialWithLabels:
                    format = "jpg";
                    break;
                default:
                    format = "png";
                    break;
            }

            return format;
        }

        public int GetDownloadWidth(int actualWidth, int actualHeight)
        {
            if (actualWidth > actualHeight)
            {
                return Math.Max(Math.Min(actualWidth, MAX_WIDTH), MAX_WIDTH);
            }
            else
            {
                // Scale height to MAX_HEIGHT and width proportionately
                double scale = (double)MAX_HEIGHT / Math.Min(actualHeight, MAX_HEIGHT);
                return (int)(actualWidth * scale);
            }
        }

        public int GetDownloadHeight(int actualWidth, int actualHeight)
        {
            if (actualWidth > actualHeight)
            {
                // Scale width to MAX_WIDTH and height proportionately
                double scale = (double)MAX_WIDTH / Math.Min(actualWidth, MAX_WIDTH);
                return (int)(actualHeight * scale);
            }
            else
            {
                return Math.Max(Math.Min(actualHeight, MAX_HEIGHT), MAX_HEIGHT);
            }
        }

        public abstract string BuildGeocodeRequestUrl(string address);

        public abstract string BuildImageRequestUrl(AerialImageData imageData, bool update = false);
    }
}
