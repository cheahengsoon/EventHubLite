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
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;
using Coding4Fun.Phone.Controls;

namespace EventHubLite
{
    public static class ReviewBugger
    {
        private const int numOfRunsBeforeFeedback = 5;
        private static int numOfRuns = -1;
        private static readonly IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        private static readonly Button yesButton = new Button() { Content = "Yes", Width = 120 };
        private static readonly Button laterButton = new Button() { Content = "Later", Width = 120 };
        private static readonly Button neverButton = new Button() { Content = "Never", Width = 120 };
        private static readonly MessagePrompt messagePrompt = new MessagePrompt();

        public static void CheckNumOfRuns()
        {
            if (!settings.Contains("numOfRuns"))
            {
                numOfRuns = 1;
                settings.Add("numOfRuns", 1);
            }
            else if (settings.Contains("numOfRuns") && (int)settings["numOfRuns"] != -1)
            {
                settings.TryGetValue("numOfRuns", out numOfRuns);
                numOfRuns++;
                settings["numOfRuns"] = numOfRuns;
            }
        }

        public static void DidReview()
        {
            if (settings.Contains("numOfRuns"))
            {
                numOfRuns = -1;
                settings["numOfRuns"] = -1;
            }
        }

        public static bool IsTimeForReview()
        {
            return numOfRuns % numOfRunsBeforeFeedback == 0 ? true : false;
        }

        public static void PromptUser()
        {
            yesButton.Click += new RoutedEventHandler(yesButton_Click);
            laterButton.Click += new RoutedEventHandler(laterButton_Click);
            neverButton.Click += new RoutedEventHandler(neverButton_Click);

            messagePrompt.Message = "Good rates and reviews encourage me to create and release updates for this app. Would you like to rate Event Hub now?";

            messagePrompt.ActionPopUpButtons.RemoveAt(0);
            messagePrompt.ActionPopUpButtons.Add(yesButton);
            messagePrompt.ActionPopUpButtons.Add(laterButton);
            messagePrompt.ActionPopUpButtons.Add(neverButton);
            messagePrompt.Show();
        }

        static void yesButton_Click(object sender, RoutedEventArgs e)
        {
            var review = new MarketplaceReviewTask();
            review.Show();
            messagePrompt.Hide();
            DidReview();
        }

        static void laterButton_Click(object sender, RoutedEventArgs e)
        {
            numOfRuns = -1;
            messagePrompt.Hide();
        }

        static void neverButton_Click(object sender, RoutedEventArgs e)
        {
            DidReview();
            messagePrompt.Hide();
        }
    }
}
