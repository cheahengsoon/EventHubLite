#define DEBUG_AGENT

using System.Windows;
using Microsoft.Phone.Scheduler;
using System.IO.IsolatedStorage;
using System.Net;
using System;
using EventViewModelLibrary;
using System.Xml.Linq;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Phone.Shell;

namespace LiveTileAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            // If debugging is enabled, launch the agent again in one minute.
#if DEBUG_AGENT
  ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(60));
#endif
            //TODO: Add code to perform your task in background
            if (task is PeriodicTask)
                createTile();
            else
            {
                NotifyComplete();
            }
        }

        void createTile()
        {
            string uri = "http://api.eventful.com/rest/events/search?app_key=gPKJF3QwZcxB5FWF";

            uri += "&location=" + settings["LocationName"].ToString();
            uri += "&within=" + settings["searchWithin"].ToString() + "&units=" + settings["KmOrMi"].ToString();

            string categoryId = settings["LiveTileCategoryId"].ToString();

            if (categoryId != "")
            {
                uri += "&category=" + categoryId;
            }

            uri += "&date=" + settings["LiveTileEvent"].ToString() + "&page_size=1&sort_order=popularity";

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

                    StandardTileData UpdateTileData = new StandardTileData
                    {
                        BackgroundImage = new Uri(tileEvent.DisplayImageSource, UriKind.RelativeOrAbsolute),
                        Title = tileEvent.Title,
                        BackTitle = tileEvent.VenueName,
                        BackContent = tileEvent.displayEventTime
                    };

                    settings["EventClicked"] = tileEvent;
                    settings.Save();

                    ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("TileID=2"));

                    // If the Tile was found, then update it.
                    if (TileToFind != null)
                    {
                        TileToFind.Update(UpdateTileData);
                    }
                }
            }
            NotifyComplete();
        }
    }
}