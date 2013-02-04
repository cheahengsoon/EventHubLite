using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Net;
using System.Linq;
using EventViewModelLibrary;

namespace EventHubLite
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.topEvents = new ObservableCollection<eventsViewModel>();
            this.Categories = new ObservableCollection<CategoryViewModel>();
            this.upcomingEvents = new ObservableCollection<eventsViewModel>();
            this.todayEvents = new ObservableCollection<eventsViewModel>();
            this.favouriteEvents = new ObservableCollection<eventsViewModel>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<eventsViewModel> topEvents { get; private set; }

        public ObservableCollection<CategoryViewModel> Categories { get; private set; }

        public ObservableCollection<eventsViewModel> upcomingEvents { get; private set; }

        public ObservableCollection<eventsViewModel> todayEvents { get; private set; }

        public ObservableCollection<eventsViewModel> favouriteEvents { get; private set; }

        //private string _sampleProperty = "Sample Runtime Property Value";
        ///// <summary>
        ///// Sample ViewModel property; this property is used in the view to display its value using a Binding
        ///// </summary>
        ///// <returns></returns>
        //public string SampleProperty
        //{
        //    get
        //    {
        //        return _sampleProperty;
        //    }
        //    set
        //    {
        //        if (value != _sampleProperty)
        //        {
        //            _sampleProperty = value;
        //            NotifyPropertyChanged("SampleProperty");
        //        }
        //    }
        //}

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public bool IsUpcomingEventLoaded
        {
            get;
            set;
        }

        public bool IsTodayEventLoaded
        {
            get;
            set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            ApiAccess.LoadCategories();
            
            this.IsDataLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}