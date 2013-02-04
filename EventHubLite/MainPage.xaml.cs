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
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.GamerServices;
using System.Windows.Media.Imaging;
using EventViewModelLibrary;

namespace EventHubLite
{
    public partial class MainPage : PhoneApplicationPage
    {
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        public static PerformanceProgressBar progress;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            //Set the progress bar
            progress = new PerformanceProgressBar();
            progress.Foreground = new SolidColorBrush(Colors.Blue);
            progress.VerticalAlignment= System.Windows.VerticalAlignment.Top;
            this.LayoutRoot.Children.Add(progress);
            progress.IsIndeterminate = true;

            TiltEffect.SetIsTiltEnabled((App.Current as App).RootFrame, true);

            //Load favourites
            ObservableCollection<eventsViewModel> fav = new ObservableCollection<eventsViewModel>();
            if (settings.TryGetValue("FavouriteEvents", out fav))
            {
                foreach (var item in fav)
                {
                    App.ViewModel.favouriteEvents.Add(item);
                }
            }

            if (App.ViewModel.favouriteEvents.Count == 0)
                favourite.Visibility = System.Windows.Visibility.Visible;
            else if (App.ViewModel.favouriteEvents.Count != 0)
                favourite.Visibility = System.Windows.Visibility.Collapsed;
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private void setting_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void rate_Click(object sender, EventArgs e)
        {
            Microsoft.Phone.Tasks.MarketplaceReviewTask review = new Microsoft.Phone.Tasks.MarketplaceReviewTask();
            review.Show();
        }

        private void feedback_Click(object sender, EventArgs e)
        {
            Microsoft.Phone.Tasks.EmailComposeTask compose = new Microsoft.Phone.Tasks.EmailComposeTask();
            compose.To = "wp7student@hotmail.com";
            compose.Subject = "Feedback for Event Hub Lite";
            compose.Show();
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot.SelectedIndex == 1) // Upcoming events
            {
                if (App.ViewModel.IsUpcomingEventLoaded != true)
                {
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
                    ApiAccess.eventsSearch(categoryId, "Next 30 days", "popularity", "Upcoming");
                    progress.IsIndeterminate = true;
                }
            }

            if (MainPivot.SelectedIndex == 2) // Today Event
            {
                if (App.ViewModel.IsTodayEventLoaded != true)
                {
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
                    ApiAccess.eventsSearch(categoryId, "Today", "popularity", "TodayEvents");
                    progress.IsIndeterminate = true;
                }
            }
        }

        private void categoryName_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            CategoryViewModel cat = (sender as TextBlock).DataContext as CategoryViewModel;
            NavigationService.Navigate(new Uri("/Category.xaml?id=" + cat.Id + "&name=" + cat.Name, UriKind.Relative));
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            eventsViewModel selectedEvent = (sender as StackPanel).DataContext as eventsViewModel;
            settings["EventClicked"] = selectedEvent;
            NavigationService.Navigate(new Uri("/EventDetails.xaml", UriKind.Relative));
        }

        private void remove_Click(object sender, RoutedEventArgs e)
        {
            eventsViewModel selectedEvent = (sender as MenuItem).DataContext as eventsViewModel;

            //Remove from favourites
            App.ViewModel.favouriteEvents.Remove(selectedEvent);

            //Update favourites in IsolatedStorageSettings
            settings["FavouriteEvents"] = App.ViewModel.favouriteEvents;
        }

        private void search_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Search.xaml", UriKind.Relative));
        }

        private void about_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void liveTile_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/LiveTile.xaml", UriKind.Relative));
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.favouriteEvents.Count == 0)
                favourite.Visibility = System.Windows.Visibility.Visible;
            else if (App.ViewModel.favouriteEvents.Count != 0)
                favourite.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void adRemoval_Click(object sender, EventArgs e)
        {
            Microsoft.Phone.Tasks.MarketplaceDetailTask buyApp = new Microsoft.Phone.Tasks.MarketplaceDetailTask();
            buyApp.ContentIdentifier = "42a50b74-aa1b-4416-9182-434e97f1fe86";
            buyApp.Show();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ReviewBugger.IsTimeForReview())
            {
                ReviewBugger.PromptUser();
            }
        }
    }
}