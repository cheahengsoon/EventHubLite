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
using System.Threading;
using EventViewModelLibrary;

namespace EventHubLite
{
    public partial class Category : PhoneApplicationPage
    {
        public static ObservableCollection<eventsViewModel> TopEvents { get; private set; }
        public static ObservableCollection<eventsViewModel> ThisWeekEvents { get; private set; }
        public static ObservableCollection<eventsViewModel> TodayEvents { get; private set; }
        public static PerformanceProgressBar progress;
        public static bool isTopEventsLoaded { get; set; }
        public static bool isThisWeekEventsLoaded { get; set; }
        public static bool isTodayEventsLoaded { get; set; }
        string id;

        public Category()
        {
            InitializeComponent();
            TopEvents = new ObservableCollection<eventsViewModel>();
            ThisWeekEvents = new ObservableCollection<eventsViewModel>();
            TodayEvents = new ObservableCollection<eventsViewModel>();

            //Set the progress bar
            progress = new PerformanceProgressBar();
            progress.Foreground = new SolidColorBrush(Colors.Blue);
            progress.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            this.LayoutRoot.Children.Add(progress);

            TiltEffect.SetIsTiltEnabled((App.Current as App).RootFrame, true);

            FirstListBox.ItemsSource = TodayEvents;
            SecondListBox.ItemsSource = ThisWeekEvents;
            ThirdListBox.ItemsSource = TopEvents;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string name = "";
            if (NavigationContext.QueryString.TryGetValue("name", out name))
                categoryPivot.Title = name;
            
            bool success = NavigationContext.QueryString.TryGetValue("id", out id);

            //Call eventsSearch in ApiAccess to search today events (as today is the default pivot item loaded
            //ApiAccess.eventsSearch(id, "Today", "popularity", "CategoryTodayEvents");
            //Use threading to make it faster.
            Dispatcher.BeginInvoke(new ThreadStart(executeToday));
            Dispatcher.BeginInvoke(new ThreadStart(exectueWeek));
            Dispatcher.BeginInvoke(new ThreadStart(executeTop));
            base.OnNavigatedTo(e);
        }

        void exectueWeek()
        {
            if (isThisWeekEventsLoaded != true)
                ApiAccess.eventsSearch(id, "This Week", "popularity", "CategoryThisWeekEvents");
        }

        void executeTop()
        {
            if (isTopEventsLoaded != true)
                ApiAccess.eventsSearch(id, "Future", "popularity", "CategoryTopEvents");
        }

        void executeToday()
        {
            if (isTodayEventsLoaded != true)
            {
                ApiAccess.eventsSearch(id, "Today", "popularity", "CategoryTodayEvents");
                progress.IsIndeterminate = true;
            }
        }

        //private void category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //if (categoryPivot.SelectedIndex == 1) //This week
        //    //{
        //    //    if (isThisWeekEventsLoaded != true)
        //    //    {
        //    //        ApiAccess.eventsSearch(id, "This Week", "popularity", "CategoryThisWeekEvents");
        //    //        progress.IsIndeterminate = true;
        //    //    }
        //    //}

        //    //if (categoryPivot.SelectedIndex == 2) //Top events
        //    //{
        //    //    if (isTopEventsLoaded != true)
        //    //    {
        //    //        ApiAccess.eventsSearch(id, "Future", "popularity", "CategoryTopEvents");
        //    //        progress.IsIndeterminate = true;
        //    //    }
        //    //}
        //}

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Uri.OriginalString == "/MainPage.xaml")
            {
                isThisWeekEventsLoaded = false;
                isTopEventsLoaded = false;
                isTodayEventsLoaded = false;
            }
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