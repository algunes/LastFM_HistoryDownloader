using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using Polly;

namespace WpfTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static HttpClient client = new HttpClient();
        public string TargetFolderPath { get; set; }
        public string Username { get; set; }
        public string API_Key { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            APIKey.Text = "0c9bc782ceac1613c25afb3237fe8d8b";
            this.DataContext = this;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Username = UserName.Text;
            TargetFolderPath = TargetPath.Text;
            API_Key = APIKey.Text;
            DownloadIndicator.Text = "Recent Tracks Downloading...";
            _ = await StartRecentTracksDownloading(TargetFolderPath, Username, API_Key);
            DownloadIndicator.Text = "Artist Library Downloading...";
            _ = await StartArtistDownloading(TargetFolderPath, Username, API_Key);
            DownloadIndicator.Text = "Finished";
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog();
            _ = dialog.ShowDialog();
            TargetFolderPath = dialog.SelectedPath;
            TargetPath.Text = TargetFolderPath;
        }

        private async Task<bool> StartArtistDownloading(string TargetFolderPath, string Username, string APIKey)
        {
            return await GetArtists(TargetFolderPath, Username, APIKey);
        }

        private async Task<bool> StartRecentTracksDownloading(string TargetFolderPath, string Username, string APIKey)
        {

            return await GetRecentTracks(TargetFolderPath, Username, APIKey);
        }

        static async Task<bool> GetArtists(string TargetFolderPath, string Username, string APIKey)
        {
            int counter = 1;
            int WriteLineCounter = 0;
            int TotalPage;
            int Page;
            bool ControlFlag = true;
            string responseString = "";
            System.IO.StreamWriter file1 = new System.IO.StreamWriter(TargetFolderPath + "\\" + Username + "_LastFM_Artists.csv", true, Encoding.UTF8);
            await file1.WriteLineAsync("Artist,Scrobbles");
            while (ControlFlag)
            {

                await Policy.Handle<Exception>().RetryAsync(5, (e, r) => { Console.WriteLine("Retrying..."); }).ExecuteAsync(async () =>
                {
                    responseString = await client.GetStringAsync("http://ws.audioscrobbler.com/2.0/?method=library.getartists&user=" + Username + "&api_key=" + APIKey + "&limit=2000&page=" + counter + "&format=xml");
                });

                XElement artists = XElement.Parse(responseString);

                IEnumerable<XElement> artistList = artists.Descendants("artists").Descendants("name");
                IEnumerable<XElement> artistCountList = artists.Descendants("artists").Descendants("playcount");

                TotalPage = int.Parse(artists.Element("artists").Attribute("totalPages").Value);
                Page = int.Parse(artists.Element("artists").Attribute("page").Value);



                foreach (XElement xe in artistList)
                {
                    await file1.WriteLineAsync(xe.Value.Replace(",", "") + "," + artistCountList.ElementAt(WriteLineCounter).Value);
                    WriteLineCounter++;
                }
                Console.WriteLine("Total fetched item number: " + artistList.Count<XElement>());

                counter++;
                if (TotalPage < counter)
                {
                    ControlFlag = false;
                }
                WriteLineCounter = 0;


            }
            file1.Close();
            return true;

        }

        static async Task<bool> GetRecentTracks(string TargetFolderPath, string Username, string APIKey)
        {
            int counter = 1;
            int WriteLineCounter = 0;
            int Page;
            int TotalPage;
            bool ControlFlag = true;
            string responseString = "";
            System.IO.StreamWriter file1 = new System.IO.StreamWriter(TargetFolderPath + "\\" + Username + "_LastFM_RecentTracks.csv", true, Encoding.UTF8);
            await file1.WriteLineAsync("Artist,Track,Album,DateTime");
            while (ControlFlag)
            {
                await Policy.Handle<Exception>().RetryAsync(5, (e, r) => { Console.WriteLine("Retrying..."); }).ExecuteAsync(async () =>
                {
                    responseString = await client.GetStringAsync("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + Username + "&api_key=" + APIKey + "&limit=1000&page=" + counter + "&format=xml");
                });
                

                XElement recentTracks = XElement.Parse(responseString);

                IEnumerable<XElement> recentTracksArtistList = recentTracks.Descendants("recenttracks").Descendants("artist");
                IEnumerable<XElement> recentTracksList = recentTracks.Descendants("recenttracks").Descendants("name");
                IEnumerable<XElement> recentTracksAlbumList = recentTracks.Descendants("recenttracks").Descendants("album");
                IEnumerable<XElement> recentTracksDateList = recentTracks.Descendants("recenttracks").Descendants("date");

                TotalPage = int.Parse(recentTracks.Element("recenttracks").Attribute("totalPages").Value);
                Page = int.Parse(recentTracks.Element("recenttracks").Attribute("page").Value);

                foreach (XElement xe in recentTracksArtistList)
                {
                    await file1.WriteLineAsync(xe.Value.Replace(",", "") + "," + recentTracksList.ElementAt(WriteLineCounter).Value.Replace(",", "") + "," + recentTracksAlbumList.ElementAt(WriteLineCounter).Value.Replace(",", "") + "," + recentTracksDateList.ElementAt(WriteLineCounter).Value.Replace(",", ""));
                    WriteLineCounter++;
                }

                counter++;
                if (TotalPage < counter)
                {
                    ControlFlag = false;
                }
                WriteLineCounter = 0;

            }

            file1.Close();
            return true;

        }
    }
}
