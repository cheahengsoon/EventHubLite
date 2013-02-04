using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using System.Collections.Generic;
using EventViewModelLibrary;
using System.IO;

namespace EventHubLite
{
    public static class ApiAccess
    {

        public static void eventsSearch(string category, string date, string sortOrder, string type)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            string uri = "http://api.eventful.com/rest/events/search?app_key=" + App.ApiKey;

            //Search by location
            if (settings["useLocation"].ToString() == "On")
            {
                //Bug fix : UseLocation might have been turned on when internet connection was not avaliable and hence these 2 keys would not have been set!
                try
                {
                    uri += "&location=" + settings["LocationName"].ToString();
                    uri += "&within=" + settings["searchWithin"].ToString() + "&units=" + settings["KmOrMi"].ToString();
                }
                catch (Exception)
                {
                    settings["useLocation"] = "Off";
                }
            }

            //If a specific category is selected then we need to get the id
            if (category != "")
            {
                uri += "&category=" + category;
            }

            uri += "&date=" + date + "&page_size=25&sort_order=" + sortOrder;

            callWebService(uri, type);
        }

        public static void LoadCategories()
        {
            callWebService("http://api.evdb.com/rest/categories/list?app_key=" + App.ApiKey, "categories");
        }

        static void callWebService(string uri, string type)
        {
            var client = new WebClient();
            if (type == "categories")
                client.DownloadStringCompleted += downloadCategories;
            if (type == "Topevents")
                client.DownloadStringCompleted += downloadTopEvents;
            if (type == "Upcoming")
                client.DownloadStringCompleted += downloadUpcoming;
            if (type == "TodayEvents")
                client.DownloadStringCompleted += downloadTodayEvents;
            if(type=="CategoryTopEvents")
                client.DownloadStringCompleted += downloadCategoryTopEvents;
            if (type == "CategoryTodayEvents")
                client.DownloadStringCompleted += downloadCategoryTodayEvents;
            if (type == "CategoryThisWeekEvents")
                client.DownloadStringCompleted += downloadCategoryThisWeekEvents;
            if (type == "SearchByKeyword")
                client.DownloadStringCompleted += downloadKeywordSearch;
            client.DownloadStringAsync(new Uri(uri));
        }

        static void downloadCategories(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
                string result = ev.Result;
                string val;
                XDocument xdoc = XDocument.Parse(result);
                var query = from p in xdoc.Elements("categories").Elements("category")
                            select p;
                foreach (var record in query)
                {
                    val = (record.Element("name").Value.Split(' '))[0];
                    App.ViewModel.Categories.Add(new CategoryViewModel() { Name = val, Id = record.Element("id").Value });
                }
            }
            else
            {
                MainPage.progress.IsIndeterminate = false;
            }

            //Wait till the categories have been fetched from the website.
            //Only after that can the top events be downloaded as the user might have selected to show events
            //from a specific category
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            //Load top events! Hence sort by popularity
            string category = settings["showCategory"].ToString();
            string categoryId = "";
            foreach (var item in App.ViewModel.Categories)
            {
                if (item.Name == category)
                {
                    categoryId = item.Id;
                    break;
                }
            }
            ApiAccess.eventsSearch(categoryId, "Future", "popularity", "Topevents");
        }

        static void downloadTopEvents(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
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
                        //No image for this event
                        imageMedium = "/Images/no_image.png";
                    }

                    //Get all performers details
                    List<Performers> performers = new List<Performers>();
                    var newQuery = from x in record.Elements("performers").Elements("performer")
                                   select x;
                    foreach (var item in newQuery)
                    {
                        performers.Add(new Performers() { Id = item.Element("id").Value, Name = item.Element("name").Value, ShortBio = item.Element("short_bio").Value, url = new Uri(item.Element("url").Value) });
                    }

                    //Combine Venue name and City name for ease of display.
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

                    App.ViewModel.topEvents.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value +", " + record.Element("city_name").Value });
                }

                //Store in XML
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var file = store.OpenFile("cachedEvents.xml", FileMode.Create, FileAccess.Write);
                    xdoc.Save(file);
                    file.Close();
                }
            }

            else
            {
                //bug fix : have to place this in try and ctach block so that app doesn't crash when it is loaded for the first time it is run, it is run without an internet connection
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        var file = store.OpenFile("cachedEvents.xml", FileMode.Open, FileAccess.Read);
                        XDocument xdoc = XDocument.Load(file);

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
                                //No image for this event
                                imageMedium = "/Images/no_image.png";
                            }

                            //Get all performers details
                            List<Performers> performers = new List<Performers>();
                            var newQuery = from x in record.Elements("performers").Elements("performer")
                                           select x;
                            foreach (var item in newQuery)
                            {
                                performers.Add(new Performers() { Id = item.Element("id").Value, Name = item.Element("name").Value, ShortBio = item.Element("short_bio").Value, url = new Uri(item.Element("url").Value) });
                            }

                            //Combine Venue name and City name for ease of display.
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

                            App.ViewModel.topEvents.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value });
                        }
                    }
                }
                catch (Exception)
                {
                   
                }
            }

            MainPage.progress.IsIndeterminate = false;
        }

        static void downloadUpcoming(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
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
                        //No image for this event
                        imageMedium = "/Images/no_image.png";
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

                    App.ViewModel.upcomingEvents.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value });
                }
            }

            MainPage.progress.IsIndeterminate = false;

            App.ViewModel.IsUpcomingEventLoaded = true;
        }

        static void downloadTodayEvents(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
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
                        //No image for this event
                        imageMedium = "/Images/no_image.png";
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

                    App.ViewModel.todayEvents.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value });
                }
            }

            MainPage.progress.IsIndeterminate = false;

            App.ViewModel.IsTodayEventLoaded = true;
        }

        static void downloadCategoryTopEvents(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
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
                        //No image for this event
                        imageMedium = "/Images/no_image.png";
                    }

                    //Get all performers details
                    List<Performers> performers = new List<Performers>();
                    var newQuery = from x in record.Elements("performers").Elements("performer")
                                   select x;
                    foreach (var item in newQuery)
                    {
                        performers.Add(new Performers() { Id = item.Element("id").Value, Name = item.Element("name").Value, ShortBio = item.Element("short_bio").Value, url = new Uri(item.Element("url").Value) });
                    }

                    //Combine Venue name and City name for ease of display.
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

                    Category.TopEvents.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value });
                }
            }

            //Category.progress.IsIndeterminate = false;
            Category.isTopEventsLoaded = true;
        }

        static void downloadCategoryTodayEvents(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
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
                        //No image for this event
                        imageMedium = "/Images/no_image.png";
                    }

                    //Get all performers details
                    List<Performers> performers = new List<Performers>();
                    var newQuery = from x in record.Elements("performers").Elements("performer")
                                   select x;
                    foreach (var item in newQuery)
                    {
                        performers.Add(new Performers() { Id = item.Element("id").Value, Name = item.Element("name").Value, ShortBio = item.Element("short_bio").Value, url = new Uri(item.Element("url").Value) });
                    }

                    //Combine Venue name and City name for ease of display.
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

                    Category.TodayEvents.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value });
                }
            }

            Category.progress.IsIndeterminate = false;
            Category.isTodayEventsLoaded = true;
        }

        static void downloadCategoryThisWeekEvents(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
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
                        //No image for this event
                        imageMedium = "/Images/no_image.png";
                    }

                    //Get all performers details
                    List<Performers> performers = new List<Performers>();
                    var newQuery = from x in record.Elements("performers").Elements("performer")
                                   select x;
                    foreach (var item in newQuery)
                    {
                        performers.Add(new Performers() { Id = item.Element("id").Value, Name = item.Element("name").Value, ShortBio = item.Element("short_bio").Value, url = new Uri(item.Element("url").Value) });
                    }

                    //Combine Venue name and City name for ease of display.
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

                    Category.ThisWeekEvents.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value });
                }
            }

            //Category.progress.IsIndeterminate = false;
            Category.isThisWeekEventsLoaded = true;
        }

        public static void searchByKeyword(bool useLocation, string keyword, string type)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            string uri = "http://api.eventful.com/rest/events/search?app_key=" + App.ApiKey;

            //If user has asked to search for event in his area
            if (useLocation==true)
            {
                uri += "&location=" + settings["LocationName"].ToString();
                uri += "&within=" + settings["searchWithin"].ToString() + "&units=" + settings["KmOrMi"].ToString();
            }

            //set keyword of search
            uri += "&date=Future&keywords=" + keyword;
            callWebService(uri, type);
        }

        static void downloadKeywordSearch(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null && ev.Result!="")
            {
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
                        //No image for this event
                        imageMedium = "/Images/no_image.png";
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

                    Search.SearchResults.Add(new eventsViewModel() { Description = record.Element("description").Value, DisplayImageSource = imageMedium, Id = record.Attribute("id").Value, Location = loc, Performers = performers, startTime = record.Element("start_time").Value, Title = record.Element("title").Value, Url = new Uri(record.Element("url").Value), VenueName = record.Element("venue_name").Value + ", " + record.Element("city_name").Value });
                }
            }

            Search.progress.IsIndeterminate = false;
        }
    }
}
