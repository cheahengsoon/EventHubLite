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
using System.Windows.Media.Imaging;
using HtmlAgilityPack;
using System.IO;
using Microsoft.Xna.Framework.GamerServices;
using System.Xml.Linq;
using EventViewModelLibrary;

namespace EventHubLite
{
    public partial class EventDetails : PhoneApplicationPage
    {
        public eventsViewModel currentEvent;
        public PerformanceProgressBar progress;
        string ticketURL="";

        public EventDetails()
        {
            InitializeComponent();

            TiltEffect.SetIsTiltEnabled((App.Current as App).RootFrame, true);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            
            currentEvent = (eventsViewModel)settings["EventClicked"];
            EventDetailsPivot.Title = currentEvent.Title;

            //load all the details onto the page
            loadDetails();

            base.OnNavigatedTo(e);
        }

        void loadDetails()
        {
            //Load PivotItem about
            eventImage.Source = new BitmapImage(new Uri(currentEvent.DisplayImageSource, UriKind.RelativeOrAbsolute));
            eventName.Text = currentEvent.Title;
            venueDetails.Text = currentEvent.VenueName;
            dateDetails.Text = currentEvent.displayEventTime;
            if (currentEvent.Description == "")
                description.Text = "Details unavaliable";
            else
            {
                //Used from HtmlAgilityPack. Can ignore understanding the working
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(currentEvent.Description);

                StringWriter sw = new StringWriter();
                ConvertTo(doc.DocumentNode, sw);
                sw.Flush();
                string desc = sw.ToString();
                description.Text = desc;
            }

            //Load PivotItem performers
            if (currentEvent.Performers.Count != 0)
            {
                performerDetails.Visibility = System.Windows.Visibility.Collapsed;
                FirstListBox.ItemsSource = currentEvent.Performers;
            }
            else
            {
                performerDetails.Visibility = System.Windows.Visibility.Visible;
                FirstListBox.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }

        public void ConvertTo(HtmlNode node, TextWriter outText)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "p":
                            // treat paragraphs as crlf
                            outText.Write("\r\n");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    break;
            }
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Performers selectedPerformer = (sender as StackPanel).DataContext as Performers;
            NavigationService.Navigate(new Uri("/Performer.xaml?id=" + selectedPerformer.Id, UriKind.Relative));
        }

        private void share_Click(object sender, EventArgs e)
        {
            Microsoft.Phone.Tasks.ShareLinkTask linkShare = new Microsoft.Phone.Tasks.ShareLinkTask();
            linkShare.Message = "Check out this event : ";
            linkShare.LinkUri = currentEvent.Url;
            linkShare.Show();
        }

        private void email_Click(object sender, EventArgs e)
        {
            Microsoft.Phone.Tasks.EmailComposeTask emailEvent = new Microsoft.Phone.Tasks.EmailComposeTask();
            emailEvent.Body = "Check out this event : " + currentEvent.Url.ToString();
            emailEvent.Subject = "[Event Hub] Event : " + currentEvent.Title;
            emailEvent.Show();
        }

        private void price_Click(object sender, EventArgs e)
        {
            //Set the progress bar
            progress = new PerformanceProgressBar();
            progress.Foreground = new SolidColorBrush(Colors.Blue);
            progress.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            this.LayoutRoot.Children.Add(progress);
            progress.IsIndeterminate = true;

            //Call the webservice to get the price
            string uri = "http://api.evdb.com/rest/events/get?app_key=" + App.ApiKey + "&id=" + currentEvent.Id;
            var client = new WebClient();
            client.DownloadStringCompleted += downloadCompleted;
            client.DownloadStringAsync(new Uri(uri));
        }

        void downloadCompleted(object s, DownloadStringCompletedEventArgs ev)
        {
            progress.IsIndeterminate = false;
            if (ev.Error == null)
            {
                string result = ev.Result;
                string free;
                XDocument xdoc = XDocument.Parse(result);
                free = xdoc.Element("event").Element("free").Value;
                if (free == "1")
                    Guide.BeginShowMessageBox("Price?", "This event is free", new List<string> { "Okay" }, 0, MessageBoxIcon.Alert, null, null);
                
                else
                {
                    string price = xdoc.Element("event").Element("price").Value;

                    var query = from p in xdoc.Elements("event").Elements("links").Elements("link")
                                select p;
                    foreach (var record in query)
                    {
                        if (record.Element("type").Value == "Tickets")
                        {
                            ticketURL = record.Element("url").Value;
                            break;
                        }
                    }

                    if (price == "" && ticketURL == "") //i.e. price and ticketUrl both unavaliable
                        Guide.BeginShowMessageBox("Price?", "Price information is unavaliable for this event", new List<string> { "Okay" }, 0, MessageBoxIcon.Alert, null, null);
                    
                    else if (price=="" && ticketURL!="")
                    {
                        Guide.BeginShowMessageBox("Price?", "Detailed price info unavaliable but a ticket link is avaliable!", new List<string> { "Check website", "Okay" }, 0, MessageBoxIcon.Alert, buyTicket, null);
                    }

                    else
                    {
                        Guide.BeginShowMessageBox("Price?", price, new List<string> { "Buy ticket", "Okay" }, 0, MessageBoxIcon.Alert, buyTicket, null);
                    }
                }
            }
            else
            {
                Guide.BeginShowMessageBox("Connectivity Error!", "Please check if you have a working data or wifi connection.", new List<string> { "Ok" }, 0, MessageBoxIcon.Alert, null, null); 
            }
        }

        private void fav_Click(object sender, EventArgs e)
        {
            foreach (var item in App.ViewModel.favouriteEvents)
            {
                if (item==currentEvent)
                {
                    Guide.BeginShowMessageBox("Error", "Event has already been marked as favourite", new List<string> { "Okay" }, 0, MessageBoxIcon.Alert, null, null);
                    return;
                }
            }
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            App.ViewModel.favouriteEvents.Add(currentEvent);
            settings["FavouriteEvents"] = App.ViewModel.favouriteEvents;

            Guide.BeginShowMessageBox("Success", "Event added to favourites", new List<string> { "Okay" }, 0, MessageBoxIcon.Alert, null, null);
        }

        private void buyTicket(IAsyncResult Result)
        {
            int? index = Guide.EndShowMessageBox(Result);
            if (index == 0)
            {
                try
                {

                    Microsoft.Phone.Tasks.WebBrowserTask task = new Microsoft.Phone.Tasks.WebBrowserTask();
                    task.Uri = new Uri(ticketURL, UriKind.Absolute);
                    task.Show();
                }
                catch (Exception)
                {
                    Guide.BeginShowMessageBox("Link unavaliable", "Sorry, link to buy ticket for this event is unavaliable!", new List<string> { "Okay" }, 0, MessageBoxIcon.Alert, null, null);
                }
            }
        }

        private void venueDetails_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                Microsoft.Phone.Tasks.BingMapsDirectionsTask launchMap = new Microsoft.Phone.Tasks.BingMapsDirectionsTask();
                launchMap.End = new Microsoft.Phone.Tasks.LabeledMapLocation(currentEvent.VenueName, new System.Device.Location.GeoCoordinate(currentEvent.Location.X, currentEvent.Location.Y));
                launchMap.Show();
            }
            catch (Exception)
            {
                Guide.BeginShowMessageBox("Co-ordinates unavaliable", "Sorry, the co-ordinates for this event is unavaliable!", new List<string> { "Okay" }, 0, MessageBoxIcon.Alert, null, null);
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}