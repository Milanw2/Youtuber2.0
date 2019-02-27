using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;
using VideoLibrary;
using System.Diagnostics;
using System.IO;
using log4net;
using System.Reflection;
using log4net.Config;

namespace Youtuber2._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string tmpDirectory = "";
        // log4net
        public static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public MainWindow()
        {
            InitializeComponent();
            RefreshPage();
            XmlConfigurator.Configure();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow SettingsWindow = new SettingsWindow();
            SettingsWindow.Show();
            _log.Info("Settings Window opened");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Application closed");
            this.Close();
        }

        private void combobox_PlaylistIDs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string playListId = "";
            playListId = comboboxPlaylistIDs.SelectedValue.ToString().Split(',')[1];
            playListId = playListId.Replace("]", "");
            _log.Info("Playlist selection changed to : " + playListId);
            // Retrieve list of all videos
            try
            {
                listbox_logging.ItemsSource = GetAllVideoTitles.GetVideoTitlesInPlayListAsync(playListId).Result;
                _log.Info("Successfully retrieved playlist items");
            }
            catch (Exception ex)
            {
                // No playlist was found prob
                _log.Fatal(ex.InnerException.Message + "There is probably something wrong with your playlist ID. See manual for more information");
            }
            
            label_amount.Content = listbox_logging.Items.Count.ToString();
        }

        private async void btnUpdateSelectedPlaylist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                disableButtons();
                await Task.Run(() => this.UpdateSelectedPlaylist(sender, e));
                enableButtons();
                stopWatch.Stop();
                _log.Info("All files where downloaded in : " + stopWatch.Elapsed + " seconds.");
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }

        private void UpdateSelectedPlaylist(object sender, RoutedEventArgs e)
        { 
            _log.Info("Update selected playlist");
            // Local variables
            string playListId = "";
            string pathMp3Files = "";
            string pathVideoFiles = "";
            string pathVideoFolder = "";
            string pathMp3Folder = "";
            string playlistTitle = "";
            string selectedPlaylistID = "";
            Boolean fileDownloaded = false;

            this.Dispatcher.Invoke(() =>
            {
                selectedPlaylistID = comboboxPlaylistIDs.SelectedValue.ToString();
            });

            // check if a playlist is selected
            if (selectedPlaylistID == null)
            {
                _log.Error("No playlist available to download or no playlist selected (impossible)");
                return;
            }
            // Check default folder is filled in
            if (Properties.Settings.Default.FilePath == null || Properties.Settings.Default.FilePath.ToString() == "")
            {
                _log.Error("No default folder is selected");
                return;
            }

            //Get Playlist title
            playlistTitle = selectedPlaylistID.Split(',')[0];
            playlistTitle = playlistTitle.Replace("[", "");
            _log.Info("Playlist title = " + playlistTitle);

            // Create folders if they don't already exists
            System.IO.Directory.CreateDirectory(Properties.Settings.Default.FilePath + "\\Youtuber");
            System.IO.Directory.CreateDirectory(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle);
            System.IO.Directory.CreateDirectory(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp");

            // Set variables
            playListId = selectedPlaylistID.Split(',')[1];
            playListId = playListId.Replace("]", "");
            pathVideoFiles  = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp\\");
            pathMp3Files    = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle + "\\");
            _log.Info("Mp3 file path = " + pathMp3Files);
            pathVideoFolder = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp");
            tmpDirectory    = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp");
            pathMp3Folder   = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle);


            // Get all video IDs from the YouTube playlist
            dynamic allVideoIds;
            try
            {
                allVideoIds = GetIDs.GetVideosInPlayListAsync(playListId).Result;
                _log.Info("Retrieved all video IDs");
            }
            catch (Exception ex)
            {
                _log.Fatal("Error retrieving all video IDs = " + ex.Message);
                return;
            }

            // Check if IDs where found
            if (allVideoIds.Count == 0)
            {
                _log.Error("Video IDs where retrieved but empty -> probably empty playlist");
                return;
            }

            // Loop over every videofile
            foreach (var videoObject in allVideoIds)
            {
                Stopwatch stopWatchFile = new Stopwatch();
                stopWatchFile.Start();

                var youtube = YouTube.Default;
                
                try
                {
                    // Get all different videos
                    var videos = YouTube.Default.GetAllVideos("http://www.youtube.com/watch?v=" + videoObject.Id);
                    _log.Info("Retrieved different video objects for specific video");

                    // Create object to store highest quality
                    VideoLibrary.YouTubeVideo videoHighRes = null;
                    int maxAudioBitrate = 0;

                    foreach (var video in videos)
                    {
                        if (video.AudioBitrate > maxAudioBitrate && (video.FileExtension == ".mp4" || video.FileExtension == ".webm"))
                        {
                            maxAudioBitrate = video.AudioBitrate;
                            videoHighRes = video;
                        }
                    }
                    _log.Info("Choosen video audio bitrate = " + videoHighRes.AudioBitrate + " for video " + videoHighRes.FullName);

                    // Write video to file if mp3 version doesn't exist yet
                    if (!File.Exists(pathMp3Files + videoHighRes.FullName.Replace(".webm", ".mp3").Replace(".mp4", ".mp3")))
                    {
                        fileDownloaded = true;
                        byte[] content = null;
                        _log.Info("File did not exist = " + pathMp3Files + videoHighRes.FullName.Replace(".webm", ".mp3").Replace(".mp4", ".mp3"));
                        for (int attempts = 0; attempts < 5; attempts++)
                        // if you really want to keep going until it works, use   for(;;)
                        {
                            try
                            {
                                _log.Info("Attempt number : " + attempts + " of file " + videoHighRes.Title);
                                content = videoHighRes.GetBytes();
                                break;
                            }
                            catch (Exception x)
                            {
                                _log.Error("Error in retry " + attempts + " with message : " + x.Message);
                            }
                            System.Threading.Thread.Sleep(1000); // Possibly a good idea to pause here
                        }

                        if (content != null)
                        {
                            _log.Info("Retrieved video data");
                            System.IO.File.WriteAllBytes(pathVideoFiles + videoHighRes.FullName, content);
                            _log.Info("Wrote file to disk = " + pathVideoFiles + videoHighRes.FullName);
                        }
                        else
                        {
                            _log.Error("See above for more info");
                            throw new System.ArgumentException("Something went wrong when retrieving the video!", "See logging for more info");
                        }
                    }
                    else
                    {
                        _log.Info("File already exists.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Info("Error during retrieving or writing video = " + ex.Message);
                }
                stopWatchFile.Stop();
                _log.Info("File downloaded in : " + stopWatchFile.Elapsed + " seconds.");
            }

            // Run batch file to convert videos to mp3 with ffmpeg
            // and delete files when done via event
            if (fileDownloaded == true)
            {
                _log.Info("All videos finished, starting VideoToMp3.bat script");
                Process proc = new Process();
                proc.Exited += new EventHandler(p_Exited);
                proc.StartInfo.FileName = "VideoToMp3.bat";
                proc.StartInfo.Arguments = String.Format("{0} {1}", pathVideoFiles, pathMp3Files);
                proc.EnableRaisingEvents = true;
                proc.Start();
            }
        }

        void p_Exited(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(tmpDirectory);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            _log.Info("tmp folder cleared");
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshPage();
            _log.Info("Page refreshed");
        }

        public void RefreshPage()
        {
            // Map Playlist IDs to playlist names
            var map = new Dictionary<string, string>();
            foreach (string item in Properties.Settings.Default.PlaylistIDs)
            {
                map.Add(GetPlaylistName.GetPlaylistNameAsync(item).Result, item);
            }

            comboboxPlaylistIDs.ItemsSource = map;
            comboboxPlaylistIDs.SelectedIndex = 0;
            _log.Info("Playlist dropdown refreshed");
        }

        private void btnUpdateAllPlaylists_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("NOT YET IMPLEMENTED", "ERROR");
        }

        private void btnGetVideoUrlFile_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Update selected playlist");
            // Local variables
            string playListId = "";
            IList<string> urlList = new List<string>();
            string playlistTitle = "";

            // check if a playlist is selected
            if (comboboxPlaylistIDs.SelectedValue == null)
            {
                _log.Error("No playlist available to download or no playlist selected (impossible)");
                return;
            }

            //Get Playlist title
            playlistTitle = comboboxPlaylistIDs.SelectedValue.ToString().Split(',')[0];
            playlistTitle = playlistTitle.Replace("[", "");
            _log.Info("Playlist title = " + playlistTitle);

            // Set variables
            playListId = comboboxPlaylistIDs.SelectedValue.ToString().Split(',')[1];
            playListId = playListId.Replace("]", "");

            // Get all video IDs from the YouTube playlist
            dynamic allVideoIds;
            try
            {
                allVideoIds = GetIDs.GetVideosInPlayListAsync(playListId).Result;
                _log.Info("Retrieved all video IDs");
            }
            catch (Exception ex)
            {
                _log.Fatal("Error retrieving all video IDs = " + ex.Message);
                return;
            }

            // Check if IDs where found
            if (allVideoIds.Count == 0)
            {
                _log.Error("Video IDs where retrieved but empty -> probably empty playlist");
                return;
            }

            // Loop over every videofile
            foreach (var videoObject in allVideoIds)
            {
                 urlList.Add("http://www.youtube.com/watch?v=" + (string)videoObject.Id);
            }

            TextWriter tw = new StreamWriter("UrlList.txt");

            foreach (String s in urlList)
            {
                tw.WriteLine(s);
            }
            tw.Close();

            Process.Start("UrlList.txt");
        }

        private void disableButtons()
        {
            btnGetVideoUrlFile.IsEnabled = false;
            btnUpdateSelectedPlaylist.IsEnabled = false;
            btnSettings.IsEnabled = false;
            btnContent.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            btnUpdateAllPlaylists.IsEnabled = false;
            comboboxPlaylistIDs.IsEnabled = false;
        }

        private void enableButtons()
        {
            btnGetVideoUrlFile.IsEnabled = true;
            btnUpdateSelectedPlaylist.IsEnabled = true;
            btnSettings.IsEnabled = true;
            btnContent.IsEnabled = true;
            btnRefresh.IsEnabled = true;
            btnUpdateAllPlaylists.IsEnabled = true;
            comboboxPlaylistIDs.IsEnabled = true;
        }
    }
}
