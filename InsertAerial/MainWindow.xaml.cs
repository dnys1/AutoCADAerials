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
using Common;
using Microsoft.Maps.MapControl.WPF;
using System.Xml;
using Path = System.IO.Path;

namespace InsertAerial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ImageSavedCallback _callback;
        private MapSource _mapSource = MapSource.Bing;
        private AerialRepository _downloadAerial = new BingAerialRepository();
        private MapType _mapType = MapType.Road;
        private string _currentDirectory;

        private bool _isMapLoaded = false;

        private AerialImageData _imageData = new AerialImageData();

        public MainWindow()
        {
            InitializeComponent();
            InitializeMap();
        }

        public MainWindow(ImageSavedCallback imageSavedCallback, string currentDirectory)
        {
            InitializeComponent();
            InitializeMap();
            _callback = imageSavedCallback;
            _currentDirectory = currentDirectory;
        }

        private void InitializeMap()
        {
            // Initialize Bing Maps
            map.ViewChangeOnFrame += new EventHandler<MapEventArgs>(Map_ViewChangeOnFrame);
            map.Center = new Location(33.44857, -112.07446);
            map.ZoomLevel = 15;

            // Initialize Google Maps
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string currDir = Path.GetDirectoryName(exePath);
            Console.WriteLine(currDir + @"\Map.html");
            wbMain.Source = new Uri(currDir + @"\Map.html");

            _isMapLoaded = true;
        }

        private void WbMain_LoadCompleted(object sender, NavigationEventArgs e)
        {
            string script = "document.body.style.overflow = 'hidden'";
            WebBrowser wb = (WebBrowser)sender;
            wb.InvokeScript("execScript", new object[] { script, "JavaScript" });
        }

        private void Map_ViewChangeOnFrame(object sender, MapEventArgs e)
        {
            map.SetView(map.Center, Math.Round(map.TargetZoomLevel));
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private GeoPoint GetMapCenter()
        {
            if (!_isMapLoaded)
            {
                return new GeoPoint(0, 0);
            }

            if (_mapSource == MapSource.Bing)
            {
                return new GeoPoint(map.Center.Latitude, map.Center.Longitude);
            } else
            {
                string center = (string)wbMain.InvokeScript("getCenter");
                double lat = double.Parse(center.Split(',')[0]);
                double lng = double.Parse(center.Split(',')[1]);

                return new GeoPoint(lat, lng);
            }
        }

        private XmlDocument GetXmlResponse(string requestUrl)
        {
            System.Diagnostics.Trace.WriteLine("Request URL (XML): " + requestUrl);
            HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));
                }
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(response.GetResponseStream());
                return xmlDoc;
            }
        }

        private void Search()
        {
            string address = addressTextBox.Text;
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            try
            {
               if (_mapSource == MapSource.Google)
                {
                    // Try to geocode the address using the Geocoder API
                    wbMain.InvokeScript("searchAddress", address);
                } else
                {
                    // Try to geocode the address using the Bing Location API
                    string geocodeRequest = _downloadAerial.BuildGeocodeRequestUrl(address);

                    XmlDocument geocodeResponse = GetXmlResponse(geocodeRequest);

                    // Get top geocode response
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(geocodeResponse.NameTable);
                    nsmgr.AddNamespace("rest", "http://schemas.microsoft.com/search/local/ws/rest/v1");

                    XmlNodeList elements = geocodeResponse.SelectNodes("//rest:Location", nsmgr);
                    if (elements.Count == 0)
                    {
                        throw new Exception("Could not locate address.");
                    }
                    else
                    {
                        XmlNodeList displayGeocodePoints =
                            elements[0].SelectNodes(".//rest:GeocodePoint/rest:UsageType[.='Display']/parent::node()", nsmgr);
                        string latitude = displayGeocodePoints[0].SelectSingleNode(".//rest:Latitude", nsmgr).InnerText;
                        string longitude = displayGeocodePoints[0].SelectSingleNode(".//rest:Longitude", nsmgr).InnerText;

                        map.Center = new Location(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
                        map.ZoomLevel = 14;

                        // Add pushpin to map
                        Pushpin pushpin = new Pushpin
                        {
                            Location = map.Center
                        };
                        map.Children.Add(pushpin);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveImage();
        }

        private string GetMapType()
        {
            if (map.Mode is RoadMode)
            {
                return "Road";
            } else
            {
                AerialMode mode = map.Mode as AerialMode;
                if (mode.Labels)
                {
                    return "AerialWithLabels";
                }

                return "Aerial";
            }
        }

        private void SaveImage()
        {
            try
            {
                int actualWidth = (int)map.ActualWidth, actualHeight = (int)map.ActualHeight;

                _imageData.Width = _downloadAerial.GetDownloadWidth(actualWidth, actualHeight);
                _imageData.Height = _downloadAerial.GetDownloadHeight(actualWidth, actualHeight);

                _imageData.Center = GetMapCenter();

                _imageData.MapType = _mapType;

                if (_mapSource == MapSource.Bing)
                {
                    _imageData.Zoom = (int)map.ZoomLevel;

                    _imageData.NECorner = new GeoPoint(map.BoundingRectangle.North, map.BoundingRectangle.East);
                    _imageData.SWCorner = new GeoPoint(map.BoundingRectangle.South, map.BoundingRectangle.West);
                } else
                {
                    _imageData.Zoom = Convert.ToInt32(wbMain.InvokeScript("getZoomLevel"));
                    
                    string bounds = (string)wbMain.InvokeScript("getBounds");

                    double north = double.Parse(bounds.Split(',')[0]);
                    double east  = double.Parse(bounds.Split(',')[1]);
                    double south = double.Parse(bounds.Split(',')[2]);
                    double west  = double.Parse(bounds.Split(',')[3]);
                    
                    _imageData.NECorner = new GeoPoint(north, east);
                    _imageData.SWCorner = new GeoPoint(south, west);
                }

                string requestUrl = _downloadAerial.BuildImageRequestUrl(_imageData);

                DownloadImage(requestUrl, _imageData.MapType);
            }
            catch (Exception ex)
            {
                string msg = "Could not get map parameters. Error " + ex.Message;
                MessageBox.Show(msg);
            }
        }

        private void DownloadImage(string requestUrl, MapType mapType)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            string format = AerialRepository.GetFormatForMapType(mapType);

            if (_currentDirectory != null)
            {
                fileDialog.InitialDirectory = _currentDirectory;
            }

            fileDialog.Filter = $"Image File (*.{format})|*.{format}";
            if (fileDialog.ShowDialog() == true)
            {
                string fileName = fileDialog.FileName;
                try
                {
                    AerialRepository.DownloadImage(requestUrl, fileName);

                    Close();

                    _imageData.FileName = fileName;
                    _callback?.Invoke(_mapSource, _imageData);
                }
                catch (Exception ex)
                {
                    string msg = "An error occurred saving the image.\n" + ex.Message + "\nPlease try again.";
                    MessageBox.Show(msg);
                }
            }
        }

        private void AddressTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search();
            }
        }

        private void SwitchMapMode_Click(object sender, RoutedEventArgs e)
        {
            if (_mapType == MapType.Road)
            {
                if (aerialLabelsCheckbox.IsChecked ?? false)
                {
                    SetMapType(MapType.AerialWithLabels);
                } else
                {
                    SetMapType(MapType.Aerial);
                }
            } else
            {
                SetMapType(MapType.Road);
            }

            if (_mapType == MapType.Aerial || _mapType == MapType.AerialWithLabels)
            {
                switchMapMode.Content = "Switch to Road";
                aerialLabelsCheckbox.IsEnabled = true;
            }
            else
            {
                switchMapMode.Content = "Switch to Aerial";
                aerialLabelsCheckbox.IsEnabled = false;
            }
        }

        private void SetMapType(MapType mapType)
        {
            switch (mapType)
            {
                case MapType.Aerial:
                    wbMain.InvokeScript("eval", new object[] { "map.setMapTypeId('satellite');" });
                    map.Mode = new AerialMode(false);
                    break;
                case MapType.AerialWithLabels:
                    wbMain.InvokeScript("eval", new object[] { "map.setMapTypeId('hybrid');" });
                    map.Mode = new AerialMode(true);
                    break;
                default:
                    wbMain.InvokeScript("eval", new object[] { "map.setMapTypeId('roadmap');" });
                    map.Mode = new RoadMode();
                    break;
            }

            _mapType = mapType;
        }

        private void AerialLabelsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (_mapType == MapType.Aerial)
            {
                map.Mode = new AerialMode(true);
                _mapType = MapType.AerialWithLabels;
                wbMain.InvokeScript("eval", new object[] { "map.setMapTypeId('hybrid');" });
            }
        }

        private void AerialLabelsCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_mapType == MapType.AerialWithLabels)
            {
                map.Mode = new AerialMode(false);
                wbMain.InvokeScript("eval", new object[] { "map.setMapTypeId('satellite');" });
                _mapType = MapType.Aerial;
            }
        }

        private void SelectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GeoPoint newCenter = GetMapCenter();
            switch (selectCombo.SelectedIndex)
            {
                case 0: // Bing
                    _downloadAerial = new BingAerialRepository();
                    if (_isMapLoaded)
                    {
                        map.Center = new Location(newCenter.lat, newCenter.lng);
                        map.ZoomLevel = Convert.ToInt32(wbMain.InvokeScript("getZoomLevel"));
                    }
                    map.Visibility = Visibility.Visible;
                    wbMain.Visibility = Visibility.Hidden;
                    _mapSource = MapSource.Bing;
                    break;
                case 1: // Google Maps
                    _downloadAerial = new GoogleMapsAerialRepository();
                    if (_isMapLoaded)
                    {
                        wbMain.InvokeScript("setCenter", $"{newCenter.lat},{newCenter.lng}");
                        wbMain.InvokeScript("setZoom", map.ZoomLevel);
                    }
                    wbMain.Visibility = Visibility.Visible;
                    map.Visibility = Visibility.Hidden;
                    _mapSource = MapSource.Google;
                    break;
            }
        }
    }
}
