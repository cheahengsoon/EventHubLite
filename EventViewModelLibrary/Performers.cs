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

namespace EventViewModelLibrary
{
    public class Performers
    {
        public string Id { get; set; }

        public Uri url { get; set; }

        public string Name { get; set; }

        public string ShortBio { get; set; }
    }
}
