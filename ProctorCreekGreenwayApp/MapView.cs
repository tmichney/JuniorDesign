﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using ZXing.Net.Mobile.Forms;
using System.Collections.Generic;
using System.ServiceModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProctorCreekGreenwayApp
{
    public class MapView : ContentPage
    {

        private static ProctorCreekMap map;
        public static List<Story> storyList = new List<Story>();
        public SearchBar searchBar;
        public StackLayout stack;

        /*
         * Generic Mapview function for both Android and IOS
         * Shown on MainPage when App is started
        */
        public MapView()
        {

            // Location of proctor creek
            var proctorCreek = new Position(33.778822, -84.439945);

            //Inititialize map centered around proctor creek
            map = new ProctorCreekMap(
                MapSpan.FromCenterAndRadius(
                    proctorCreek, Distance.FromMiles(2)))
            {
                IsShowingUser = true,
                HeightRequest = 100,
                WidthRequest = 960,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            MessagingCenter.Subscribe<ProctorCreekMap, ProctorCreekPin>(this, "Navigation", async (sender, args) =>
            {
                ProctorCreekPin pin = args;
                var page1 = new StoryPage(pin.story);
                await Navigation.PushAsync(page1);
            });

            // Initialize search bar functionality
            searchBar = new SearchBar { Placeholder = "Search", BackgroundColor = Xamarin.Forms.Color.White };
            searchBar.SearchButtonPressed += this.OnSearchClick;
            //searchBar.TextChanged += this.OnSearchTextChange;


            // Initialize QR functionality
            var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
            options.PossibleFormats = new List<ZXing.BarcodeFormat>() {
                    ZXing.BarcodeFormat.QR_CODE
            };

            var scanner = new QRScan(options);
            scanner.OnScanResult += async (result) =>
            {
                scanner.IsScanning = false;
                Device.BeginInvokeOnMainThread(async () =>
                {
                    scanner.readout = result.Text;
                    await Navigation.PopAsync();
                });
            };

            Button button = new Button() { Text = "QR" };
            button.Clicked += async (sender, e) =>
            {
                await Navigation.PushAsync(scanner);
            };

            // GUI stuff
            stack = new StackLayout { Spacing = 0 };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stack.Children.Add(searchBar);
            grid.Children.Add(map, 0, 0);
            Grid.SetRowSpan(map, 2);
            grid.Children.Add(button, 0, 1);
            stack.Children.Add(grid);
            Content = stack;
        }

        /*
         * Central database is called and the stories are retreived
         * Map is populated with pins for each location
         */
        protected async override void OnAppearing()
        {
            base.OnAppearing();

            List<Story> stories = await App.DBManager.GetStoriesAsync();

            // Loop through each story
            foreach (Story s in stories)
            {
                // Get story info
                double lat = s.Lat;
                double lng = s.Long;
                string locName = s.Name;
                int imageID = -1;

                if (s.Images.Count > 0)
                {
                    string[] split = s.Images[0].Split('/');
                    try
                    {
                        imageID = System.Convert.ToInt32(split[4]);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
                //s.ImageURL = await App.DBManager.GetImageURLAsync(s.ID);
                // Create new pin
                var pos = new Position(lat, lng);
                ProctorCreekPin pin = new ProctorCreekPin
                {
                    Label = "",
                    Position = pos,
                    story = s,
                    ImageURL = await App.DBManager.GetImageURLAsync(imageID),
                };
                pin.Clicked += this.OnLabelClick;
                map.Pins.Add(pin);
                storyList.Add(s);
            }
        }

        /*
         * Event handler for whan a pins window is clicked
         */
        async void OnLabelClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Pin clicked");

            // Figure out what pin was clicked
            ProctorCreekPin loc = (ProctorCreekPin)sender;
            string locName = loc.story.Name;
            Position latlong = loc.Position;


            // Loop through story list and find the matching story to this location
            int storyID = -1;
            Story story = null;
            foreach (Story s in storyList) {
                if (s.Name.Equals(locName)) {
                    // Update storyID
                    storyID = s.ID;
                    story = s;
                }
            }

            // FOR TESTING
            if (storyID == -1) {
                Debug.WriteLine("NO STORY FOUND; USING DEFAULT");
                storyID = 5;
                story = storyList[0];
            }

            // Direct to story page
            await Navigation.PushAsync(new StoryPage(story));
        }

        /*
         * OnSearchTextChange: didn't finish; could implement later
         */
        //async void OnSearchTextChange(Object sender, EventArgs e) {
            // Set autocomplete to be all the story names
            //ListView autocomplete = new ListView();

            // Get what user has typed so far
            //SearchBar sb = (SearchBar)sender;
            //string search = sb.Text;

            //List<string> storyNames = new List<string>();
            //foreach (Story s in storyList) {
                //if (s.Name.Contains(search)) {
                    //storyNames.Add(s.Name);
                //}
            //}
            //autocomplete.ItemsSource = storyNames;
            //stack.Children.Remove(map);
            //stack.Children.Add(autocomplete);
        //}

        async void OnSearchClick(object sender, EventArgs e)
        {
            SearchBar sb = (SearchBar)sender;
            string search = sb.Text;
            bool match = false;
            foreach (Story s in storyList) {
                if (s.Name.Equals(search)) {
                    // Direct to story page
                    await Navigation.PushAsync(new StoryPage(s));
                    match = true;
                }
             }

            if (!match) {
                // Only show this if no match
                await DisplayAlert("No Results", "No location name matches your search", "OK");
            }
           


        }
    }   
}

