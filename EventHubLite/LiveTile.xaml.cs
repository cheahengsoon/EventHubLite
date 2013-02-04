#define DEBUG_AGENT

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
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Phone.Shell;
using System.Xml.Linq;
using EventViewModelLibrary;
using System.Windows.Resources;
using System.Windows.Media.Imaging;
using Microsoft.Phone;
using System.IO;
using Microsoft.Phone.Scheduler;

namespace EventHubLite
{
    public partial class LiveTile : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        bool loading = true;
        bool generateTile = false;
        List<string> typeofEvent;

        public LiveTile()
        {
            InitializeComponent();
            typeofEvent = new List<string>() { "Top", "Today", "Upcoming" };
            displayFrom.ItemsSource = typeofEvent;
            displayFrom.SelectedItem = settings["LiveTileEvent"].ToString();

            if ((bool)settings["useLiveTile"])
                showLiveTile.IsChecked = true;
            else
            {
                showLiveTile.IsChecked = false;
                generateTile = true; //Initially the checked button is unchecked and hence next time when we check it, it means user wants to create a tile.
            }

            try
            {
                List<CategoryViewModel> displayCategories = App.ViewModel.Categories.ToList();
                displayCategories.Insert(0, new CategoryViewModel() { Name = "All Categories", Id = "" });
                category.ItemsSource = displayCategories;

                //string currentCategory = settings["LiveTileCategory"].ToString();
                string currentCategory = settings["LiveTileCategoryId"].ToString();

                var result = (from o in displayCategories
                              where o.Id == currentCategory
                              select o).ToList();
                category.SelectedItem = result[0];
            }
            catch (Exception)
            {
                Guide.BeginShowMessageBox("Connectivity Error!", "Please check if you have a working data or wifi connection.", new List<string> { "Ok" }, 0, MessageBoxIcon.Alert, null, null);
            }
            loading = false;
        }

        private void showLiveTile_Checked(object sender, RoutedEventArgs e)
        {
            //Check if location services are on
            string location = settings["useLocation"].ToString();

            if (location == "Off")
            {
                Guide.BeginShowMessageBox("Location services Error!", "'Use my location' in settings have to enabled in order to use this feature.", new List<string> { "Ok" }, 0, MessageBoxIcon.Alert, null, null);
                showLiveTile.IsChecked = false;
            }

            else
            {
                if (generateTile) //this will be true only if previously generateTile was unchecked.
                {
                    //Location services are enabled. Set up live tile.
                    settings["useLiveTile"] = true;

                    ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("TileID=2"));

                    //test if Tile was created
                    if (TileToFind == null)
                    {
                        bar.IsIndeterminate = true;
                        createTile();
                    }
                }

                else //will only reach this case when generateTile is false and during loading.
                {
                    generateTile = true;
                }
            }
        }

        //Creates the URI to fetch data from and call the dowloadCompleted Method
        void createTile()
        {
            string uri = "http://api.eventful.com/rest/events/search?app_key=" + App.ApiKey;

            uri += "&location=" + settings["LocationName"].ToString();
            uri += "&within=" + settings["searchWithin"].ToString() + "&units=" + settings["KmOrMi"].ToString();

            //string category = settings["LiveTileCategory"].ToString();
            string categoryId = settings["LiveTileCategoryId"].ToString();

            if (categoryId != "")
            {
                uri += "&category=" + categoryId;
            }

            string date;
            if (displayFrom.SelectedItem.ToString() == "Top")
                date = "Future";
            else if (displayFrom.SelectedItem.ToString() == "Today")
                date = "Today";
            else
                date = "Next 30 days";

            uri += "&date=" + date + "&page_size=1&sort_order=popularity"; //Add date (top, upcoming, future"

            var client = new WebClient();
            client.DownloadStringCompleted += downloadCompleted;
            client.DownloadStringAsync(new Uri(uri));
        }

        void downloadCompleted(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
                eventsViewModel tileEvent;

                string result = ev.Result;
                XDocument xdoc = XDocument.Parse(result);
                var query = from p in xdoc.Elements("search").Elements("events").Elements("event")
                            select p;
                foreach (var record in query)
                {
                    //Get image details
                    string imageMedium;
                    try
                    {
                        imageMedium = record.Element("image").Element("medium").Element("url").Value;
                    }
                    catch (Exception)
                    {
                        //No image for this event, then show Event Hub logo
                        imageMedium = "ApplicationIcon.png";
                    }

                    //Get all performers details
                    List<Performers> performers = new List<Performers>();
                    var newQuery = from x in record.Elements("performers").Elements("performer")
                                   select x;
                    foreach (var item in newQuery)
                    {
                        performers.Add(new Performers() { Id = item.Element("id").Value, Name = item.Element("name").Value, ShortBio = item.Element("short_bio").Value, url = new Uri(item.Element("url").Value) });
                    }

                    Point loc = new Point();
                    try
                    {
                        loc.X = Convert.ToDouble(record.Element("latitude").Value);
                    }
                    catch (Exception)
                    {
                        loc.X = -9999; // Error code for unavaliable.
                    }

                    try
                    {
                        loc.Y = Convert.ToDouble(record.Element("longitude").Value);
                    }
                    catch (Exception)
                    {
                        loc.Y = -9999; // Error code for unavaliable.
                    }

                    tileEvent = new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value };

                    //Create live tile

                    //Register background task before adding live tile.
                    try
                    {
                        PeriodicTask periodicTask = new PeriodicTask("PeriodicAgent");
                        periodicTask.Description = "Live tile for Event Hub";
                        periodicTask.ExpirationTime = System.DateTime.Now.AddDays(1);
                        
                        // If the agent is already registered with the system,
                        if (ScheduledActionService.Find(periodicTask.Name) != null)
                        {
                            ScheduledActionService.Remove("PeriodicAgent");
                        }

                        ScheduledActionService.Add(periodicTask);
#if(DEBUG_AGENT)
                        ScheduledActionService.LaunchForTest("PeriodicAgent", TimeSpan.FromSeconds(60));
#endif
                        //Create tile
                        StandardTileData NewTileData = new StandardTileData
                        {
                            BackgroundImage = new Uri(tileEvent.DisplayImageSource, UriKind.RelativeOrAbsolute),
                            Title = tileEvent.Title,
                            BackTitle = tileEvent.VenueName,
                            BackContent = tileEvent.displayEventTime
                        };

                        settings["EventClicked"] = tileEvent;

                        ShellTile.Create(new Uri("/EventDetails.xaml?TileID=2", UriKind.Relative), NewTileData);
                    }
                    catch (InvalidOperationException exception)
                    {
                        if (exception.Message.Contains("BNS Error: The action is disabled"))
                            MessageBox.Show("Background agents for this application have been disabled by the user.");
                        else
                            Guide.BeginShowMessageBox("Unable to set up live tile", "You have either reached the maximum number of background agents allowed for the phone or you are using a version of the OS which does not support background agents!", new List<string> { "Ok" }, 0, MessageBoxIcon.Error, null, null);
                        showLiveTile.IsChecked = false;
                    }

                    catch (SchedulerServiceException)
                    {
                        Guide.BeginShowMessageBox("Internal Error", "Internal Error occured. Please try again", new List<string> { "Ok" }, 0, MessageBoxIcon.Error, null, null);
                        showLiveTile.IsChecked = false;
                    }
                }
                bar.IsIndeterminate = false;
            }
        }

        private void showLiveTile_Unchecked(object sender, RoutedEventArgs e)
        {
            //Remove live tile
            settings["useLiveTile"] = false;

            ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("TileID=2"));

            // If the Tile was found, then delete it.
            if (TileToFind != null)
            {
                TileToFind.Delete();
            }

            //Also remove the periodic task as the tile has been removed.
            PeriodicTask periodicTask = new PeriodicTask("PeriodicAgent");

            // If the agent is already registered with the system,
            if (ScheduledActionService.Find(periodicTask.Name) != null)
            {
                ScheduledActionService.Remove("PeriodicAgent");
            }
        }

        private void category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                //settings["LiveTileCategory"] = ((category.SelectedItem) as CategoryViewModel).Name;
                settings["LiveTileCategoryId"] = ((category.SelectedItem) as CategoryViewModel).Id;
            }
        }

        private void displayFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                settings["LiveTileEvent"] = displayFrom.SelectedItem.ToString();
            }
        }
    }
}