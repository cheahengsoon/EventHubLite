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
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using EventViewModelLibrary;

namespace EventHubLite
{
    public partial class Performer : PhoneApplicationPage
    {
        string id;
        PerformanceProgressBar progress;

        public Performer()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            bool success = NavigationContext.QueryString.TryGetValue("id", out id);

            //Set the progress bar
            progress = new PerformanceProgressBar();
            progress.Foreground = new SolidColorBrush(Colors.Blue);
            progress.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            this.LayoutRoot.Children.Add(progress);
            progress.IsIndeterminate = true;

            //Call the webservice to get the price
            string uri = "http://api.evdb.com/rest/performers/get?app_key=" + App.ApiKey + "&id=" + id;
            var client = new WebClient();
            client.DownloadStringCompleted += downloadCompleted;
            client.DownloadStringAsync(new Uri(uri));

            base.OnNavigatedTo(e);
        }

        void downloadCompleted(object s, DownloadStringCompletedEventArgs ev)
        {
            if (ev.Error == null)
            {
                string result = ev.Result;
                XDocument xdoc = XDocument.Parse(result);
                PerformerPivot.Title = xdoc.Element("performer").Element("name").Value;
                performerName.Text = xdoc.Element("performer").Element("name").Value;
                
                Uri imageUri;
                try
                {
                    imageUri = new Uri(xdoc.Element("performer").Element("images").Element("image").Element("medium").Element("url").Value, UriKind.Absolute);
                }
                catch (Exception)
                {
                    imageUri = new Uri("/Images/no_image.png", UriKind.Relative);
                }
                performerImage.Source = new BitmapImage(imageUri);

                shortBio.Text = xdoc.Element("performer").Element("short_bio").Value;
                longBio.Text = xdoc.Element("performer").Element("long_bio").Value;

                //Comments
                var query = from p in xdoc.Elements("performer").Elements("comments").Elements("comment")
                            select p;
                string comment = "";
                foreach (var record in query)
                {
                    comment += record.Element("text").Value + " - " + record.Element("username").Value;
                    comment += "\n\n";
                }

                comments.Text = comment;
            }

            progress.IsIndeterminate = false;
        }
    }
}