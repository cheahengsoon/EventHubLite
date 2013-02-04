using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.Device.Location;
using System.Xml.Linq;
using Microsoft.Xna.Framework.GamerServices;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Globalization;

namespace EventHubLite
{
    public partial class Settings : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        GeoCoordinateWatcher watcher;
        string latitude, longitude;

        public Settings()
        {
            InitializeComponent();

            latitude = "";
            longitude = "";

            //Enable tilt effect
            TiltEffect.SetIsTiltEnabled((App.Current as App).RootFrame, true);

            //Retrieve useLocation info from IsolatedStorageSettings
            string location = settings["useLocation"].ToString();
            if (location == "On")
            {
                useLocation.IsChecked = true;
                locationOptions.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                useLocation.IsChecked = false;
                locationOptions.Visibility = System.Windows.Visibility.Collapsed;
            }

            //Set location options
            distance.Text = settings["searchWithin"].ToString();
            string KmOrMi = settings["KmOrMi"].ToString();
            if (KmOrMi == "mi")
            {
                mi.IsChecked = true;
                km.IsChecked = false;
            }
            else
            {
                km.IsChecked = true;
                mi.IsChecked = false;
            }

            //Show the current location in the location Box
            string locationName;
            settings.TryGetValue("LocationName", out locationName);
            if (locationName != null) //location was stored earlier
            {
                currentLoc.Text = locationName;
            }

            //Set category options
            try
            {
                List<CategoryViewModel> displayCategories = App.ViewModel.Categories.ToList();
                displayCategories.Insert(0, new CategoryViewModel() { Name = "All Categories", Id = "" });
                category.ItemsSource = displayCategories;
                
                string currentCategory = settings["showCategory"].ToString();

                var result = (from o in displayCategories
                              where o.Name == currentCategory
                              select o).ToList();
                category.SelectedItem = result[0];
            }
            catch (Exception)
            {
                Guide.BeginShowMessageBox("Connectivity Error!", "Please check if you have a working data or wifi connection.", new List<string> { "Ok" }, 0, MessageBoxIcon.Alert, null, null);
            }
        }

        private void useLocation_Checked(object sender, RoutedEventArgs e)
        {
            settings["useLocation"] = "On";
            locationOptions.Visibility = System.Windows.Visibility.Visible;
            string locationName;
            settings.TryGetValue("LocationName", out locationName);
            if (locationName == null) //location was not stored earlier
            {
                fetchLocationInfo();
            }
        }

        private void useLocation_Unchecked(object sender, RoutedEventArgs e)
        {
            settings["useLocation"] = "Off";
            locationOptions.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void km_Checked(object sender, RoutedEventArgs e)
        {
            //Remove the check from the other radio button
            mi.IsChecked = false;
            settings["KmOrMi"] = "km";
        }

        private void mi_Checked(object sender, RoutedEventArgs e)
        {
            //Remove the check from the other radio button
            km.IsChecked = false;
            settings["KmOrMi"] = "mi";
        }

        private void distance_TextChanged(object sender, TextChangedEventArgs e)
        {
            settings["searchWithin"] = distance.Text;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            settings["showCategory"] = ((category.SelectedItem) as CategoryViewModel).Name;
            base.OnNavigatedFrom(e);
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            fetchLocationInfo();
        }

        private void fetchLocationInfo()
        {
            bar.IsIndeterminate = true;
            //Fetch location. Show locationName in textbox. Save locationName to islotaedstoragesettings
            //Also save locationInfo to isolatedstoragesettings. This value will be in latitude and longitute format, while locationName will be the exact location name fetched from a web service.

            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // To make sure it works in all regions!
            latitude = Convert.ToString(e.Position.Location.Latitude, new CultureInfo("en-US"));
            longitude = Convert.ToString(e.Position.Location.Longitude, new CultureInfo("en-US"));
            watcher.Stop();

            //Get the actual name of these coordinates and save it in LocationName key of isolatedstoragesettings. Also show this value in the curLocation textbox
            var client = new WebClient();
            client.DownloadStringCompleted += downloadCompleted;
            string uri = "http://api.geonames.org/countrySubdivision?lat=" + latitude + "&lng=" + longitude + "&username=saurabharora90";
            client.DownloadStringAsync(new Uri(uri));
        }

        void downloadCompleted(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
                string result = ev.Result;
                XDocument xdoc = XDocument.Parse(result);
                var query = from p in xdoc.Elements("geonames").Elements("countrySubdivision")
                            select p;
                foreach (var record in query)
                {
                    try
                    {
                        //coz every place wont have a city, i.e. adminTag1, e.g. Singapore!
                        currentLoc.Text = record.Element("adminName1").Value + "," + record.Element("countryName").Value;
                        //settings["LocationName"] = record.Element("adminName1").Value + " " + record.Element("countryName").Value;
                    }
                    catch (Exception)
                    {
                        currentLoc.Text = record.Element("countryName").Value;
                        //settings["LocationName"] = record.Element("countryName").Value;
                    }
                    settings["LocationName"] = latitude + "," + longitude;
                    break;
                }
            }
            bar.IsIndeterminate = false;
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Prompt to restart app to make sure that the changes takes place
            MessageBox.Show("You need to restart the application for the changes to take effect", "Apply changes", MessageBoxButton.OK);
        }
    }
}