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
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework.GamerServices;
using EventViewModelLibrary;

namespace EventHubLite
{
    public partial class Search : PhoneApplicationPage
    {
        public static PerformanceProgressBar progress;
        public static ObservableCollection<eventsViewModel> SearchResults { get; private set; }

        public Search()
        {
            InitializeComponent();
            SearchResults = new ObservableCollection<eventsViewModel>();
            FirstListBox.ItemsSource = SearchResults;

            //Set the progress bar
            progress = new PerformanceProgressBar();
            progress.Foreground = new SolidColorBrush(Colors.Blue);
            progress.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            this.LayoutRoot.Children.Add(progress);
            //progress.IsIndeterminate = true;

            TiltEffect.SetIsTiltEnabled((App.Current as App).RootFrame, true);
        }

        private void doSearch_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            performSearch();
        }

        void performSearch()
        {
            bool continueSearch = true;

            //Check if search byLocation or search Worldwide and then call the searchByKeyword
            if (useLocation.IsChecked.Value)
            {
                IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                if (settings["useLocation"].ToString() == "Off")
                {
                    continueSearch = false;
                    Guide.BeginShowMessageBox("Error!", "Please enable and set location in setting to use this feature", new List<string> { "Ok" }, 0, MessageBoxIcon.Error, null, null); 
                }
            }
            if (continueSearch)
            {
                progress.IsIndeterminate = true;
                //Clear the search of previous results and then search
                SearchResults.Clear();
                ApiAccess.searchByKeyword(useLocation.IsChecked.Value, searchBox.Text, "SearchByKeyword");
            }
        }

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Focus();
                performSearch();
            }
        }

        private void useLocation_Checked(object sender, RoutedEventArgs e)
        {
            try //To prevent the firstTime load error
            {
                worldwide.IsChecked = false;
                //Immediate search if user has entered the keyword
                if (searchBox.Text != "")
                {
                    performSearch();
                }
            }
            catch (Exception)
            {
            }
        }

        private void worldwide_Checked(object sender, RoutedEventArgs e)
        {
            useLocation.IsChecked = false;

            //Immediate search if user has entered the keyword
            if (searchBox.Text != "")
                performSearch();
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            eventsViewModel selectedEvent = (sender as StackPanel).DataContext as eventsViewModel;
            settings["EventClicked"] = selectedEvent;
            NavigationService.Navigate(new Uri("/EventDetails.xaml", UriKind.Relative));
        }
    }
}