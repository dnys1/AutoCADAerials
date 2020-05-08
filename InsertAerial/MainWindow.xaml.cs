using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InsertAerial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string API_KEY = "AIzaSyDcQifInuUX8-7I_aqawkjQ2fzEpE32oyE";
        private static string BaseGMapsUrl = "https://maps.googleapis.com/maps/api/staticmap?";
        private bool _loadCompleted = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WbMain_LoadCompleted(object sender, NavigationEventArgs e)
        {
            string script = "document.body.style.overflow = 'hidden'";
            WebBrowser wb = (WebBrowser)sender;
            wb.InvokeScript("execScript", new object[] { script, "JavaScript" });

            _loadCompleted = true;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            if (_loadCompleted)
            {
                string address = addressTextBox.Text;
                if (string.IsNullOrEmpty(address))
                {
                    return;
                }

                try
                {
                    // Try to geocode the address using the Geocoder API
                    wbMain.InvokeScript("searchAddress", address);
                }
                catch (Exception ex)
                {
                    string msg = "Could not load address. Error " + ex.Message;
                    MessageBox.Show(msg);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveImage();
        }

        private string GetFormatForMapType(string mapType)
        {
            string format;
            switch (mapType)
            {
                case "HYBRID":
                case "SATELLITE":
                    format = "jpeg";
                    break;
                default:
                    format = "png";
                    break;
            }

            return format;
        }

        private string BuildImageRequestUrl(double lat, double lng, int zoom, string mapType)
        {
            string format = GetFormatForMapType(mapType);
            return BaseGMapsUrl + $"center={lat},{lng}&zoom={zoom}&size={(int)wbMain.ActualWidth}x{(int)wbMain.ActualHeight}" +
                $"&format={format}&maptype={mapType.ToLower()}&scale=4&key={API_KEY}";
        }

        private void SaveImage()
        {
            if (!_loadCompleted)
            {
                return;
            }
            
            try
            {
                int zoom = (int)wbMain.InvokeScript("getZoomLevel");
                string center = (string)wbMain.InvokeScript("getCenter");
                string mapType = (string)wbMain.InvokeScript("getMapType");

                double lat = double.Parse(center.Split(',')[0]);
                double lng = double.Parse(center.Split(',')[1]);

                string requestUrl = BuildImageRequestUrl(lat, lng, zoom, mapType);

                DownloadImage(requestUrl, mapType);
            }
            catch (Exception ex)
            {
                string msg = "Could not get map parameters. Error " + ex.Message;
                MessageBox.Show(msg);
            }
        }

        private string DownloadImage(string requestUrl, string mapType)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            string format = GetFormatForMapType(mapType);

            // TODO: Set initial directory
            // fileDialog.InitialDirectory = "...";

            fileDialog.Filter = $"Image File (*.{format})|*.{format}";
            if (fileDialog.ShowDialog() == true)
            {
                string fileName = fileDialog.FileName;
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(requestUrl, fileName);
                }

                Close();

                return fileName;
            }

            return null;
        }

        private void AddressTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search();
            }
        }
    }
}
