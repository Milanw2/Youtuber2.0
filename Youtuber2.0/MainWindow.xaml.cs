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
using System.Threading;

namespace Youtuber2._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: Future feature list :
        // Update total time (not resetted after finishing playlist)
        // Add logic to update all the playlist with btnUpdateAllPlaylists_Click
        // Add logging (configurable with all or only ERROR) to the UI
        // Add counter (sort of progressbar maybe?)

        string tmpDirectory = "";

        // Global variables
        string playListId;
        string pathMp3Files;
        string pathVideoFiles;
        string pathVideoFolder;
        string pathMp3Folder;
        string playlistTitle;
        string selectedPlaylistID;
        Boolean fileDownloaded;
        List<VideoObject> allVideoIds = new List<VideoObject>();
        Stopwatch total;

        API api = new API();

        // log4net
        public static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public MainWindow()
        {
            InitializeComponent();
            RefreshPage();
            XmlConfigurator.Configure();
            InitVariables();
            _log.Info("Application opened"); 
        }

        private void InitVariables()
        {
            playListId = "";
            pathMp3Files = "";
            pathVideoFiles = "";
            pathVideoFolder = "";
            pathMp3Folder = "";
            playlistTitle = "";
            selectedPlaylistID = "";
            fileDownloaded = false;
            allVideoIds.Clear();
            total = new Stopwatch();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow SettingsWindow = new SettingsWindow();
            SettingsWindow.Show();
            _log.Debug("Settings Window opened");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _log.Debug("Application closed");
            this.Close();
        }

        private void combobox_PlaylistIDs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string playListId = "";
            playListId = comboboxPlaylistIDs.SelectedValue.ToString().Split(',')[1];
            playListId = playListId.Replace("]", "");
            _log.Debug("Playlist selection changed to : " + playListId);
            // Retrieve list of all videos
            try
            {
                listbox_logging.ItemsSource = GetAllVideoTitles.GetVideoTitlesInPlayListAsync(playListId).Result;
                _log.Debug("Successfully retrieved playlist items");
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
            try
            {
                total.Start();

                // Get selected playlist ID
                this.Dispatcher.Invoke(() =>
                {
                    selectedPlaylistID = comboboxPlaylistIDs.SelectedValue.ToString();
                });

                // Default checks
                if (!DefaultChecks())
                {
                    MessageBox.Show("Default checks failed. See logging for more details.");
                    return;
                }

                // Fill variables (playlistTitle, playlistId, pathVideoFiles, pathMp3Files, pathVideoFolder, tmpDirectory, pathMp3Folder)
                // and create missing directories if they don't exist yet
                SetVariablesAndDirectories();
                
                // Get all video IDs from the YouTube playlist
                allVideoIds = GetIDs.GetVideosInPlayListAsync(playListId).Result;

                // Check if IDs where found
                if (allVideoIds.Count == 0)
                {
                    _log.Error("Video IDs where retrieved but empty -> probably empty playlist");
                    MessageBox.Show("Empty playlist?");
                    return;
                }

                // Loop over every videofile in parallel (how much threads is automatically determined based on the specs of the computer
                _log.Debug("Multi threaded retrieval is starting...");

                // Run batch file to convert videos to mp3 with ffmpeg
                // and delete files when done via event
                // if there where files downloaded
                if (api.ProcessVideosParallel(allVideoIds, pathMp3Files, fileDownloaded, pathVideoFiles))
                {
                    VideoToMp3();
                }
                // Reset variables just to be sure
                InitVariables();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error, send file 'proper.log' to me!");
                _log.Error("Fatal error: " + ex.Message);
            }
        }

        private async void btnUpdateSelectedPlaylistSlow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                disableButtons();
                await Task.Run(() => this.UpdateSelectedPlaylistSlow_(sender, e));
                enableButtons();
                stopWatch.Stop();
                _log.Info("All files where downloaded in : " + stopWatch.Elapsed + " seconds.");
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }

        private void UpdateSelectedPlaylistSlow_(object sender, RoutedEventArgs e)
        {
            try
            {
                total.Start();

                // Get selected playlist ID
                this.Dispatcher.Invoke(() =>
                {
                    selectedPlaylistID = comboboxPlaylistIDs.SelectedValue.ToString();
                });

                // Default checks
                if (!DefaultChecks())
                {
                    MessageBox.Show("Default checks failed. See logging for more details.");
                    return;
                }

                // Fill variables (playlistTitle, playlistId, pathVideoFiles, pathMp3Files, pathVideoFolder, tmpDirectory, pathMp3Folder)
                // and create missing directories if they don't exist yet
                SetVariablesAndDirectories();

                // Get all video IDs from the YouTube playlist
                allVideoIds = GetIDs.GetVideosInPlayListAsync(playListId).Result;

                // Check if IDs where found
                if (allVideoIds.Count == 0)
                {
                    _log.Error("Video IDs where retrieved but empty -> probably empty playlist");
                    MessageBox.Show("Empty playlist?");
                    return;
                }

                // Loop over every videofile in parallel (how much threads is automatically determined based on the specs of the computer
                _log.Debug("Normal slow retrieval is starting...");

                // Run batch file to convert videos to mp3 with ffmpeg
                // and delete files when done via event
                // if there where files downloaded
                if (api.ProcessVideos(allVideoIds, pathMp3Files, fileDownloaded, pathVideoFiles))
                {
                    VideoToMp3();
                }
                // Reset variables just to be sure
                InitVariables();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error, send file 'proper.log' to me!");
                _log.Error("Fatal error: " + ex.Message);
            }
        }

        private void SetVariablesAndDirectories()
        {
            //Get Playlist title
            playlistTitle = selectedPlaylistID.Split(',')[0];
            playlistTitle = playlistTitle.Replace("[", "");
            playlistTitle = playlistTitle.Replace(" ", "");
            _log.Debug("Playlist title = " + playlistTitle);

            // Create folders if they don't already exists
            System.IO.Directory.CreateDirectory(Properties.Settings.Default.FilePath + "\\Youtuber");
            System.IO.Directory.CreateDirectory(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle);
            System.IO.Directory.CreateDirectory(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp");

            // Set variables
            playListId = selectedPlaylistID.Split(',')[1];
            playListId = playListId.Replace("]", "");
            pathVideoFiles = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp\\");
            pathMp3Files = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle + "\\");
            _log.Debug("Mp3 file path = " + pathMp3Files);
            pathVideoFolder = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp");
            tmpDirectory = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\tmp");
            pathMp3Folder = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle);
        }

        private bool DefaultChecks()
        {
            // check if a playlist is selected
            if (selectedPlaylistID == null)
            {
                _log.Error("No playlist available to download or no playlist selected (impossible)");
                return false;
            }
            // Check default folder is filled in
            if (Properties.Settings.Default.FilePath == null || Properties.Settings.Default.FilePath.ToString() == "")
            {
                _log.Error("No default folder is selected");
                return false;
            }

            return true;
        }

        private void VideoToMp3()
        {
            _log.Debug("All videos finished, starting VideoToMp3.bat script");
            Process proc = new Process();
            proc.Exited += new EventHandler(process_Exited);
            proc.StartInfo.FileName = "VideoToMp3.bat";
            proc.StartInfo.Arguments = String.Format("{0} {1}", pathVideoFiles, pathMp3Files);
            proc.EnableRaisingEvents = true;
            proc.Start();
        }
        void process_Exited(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(tmpDirectory);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            total.Stop();
            _log.Debug("tmp folder cleared");
            _log.Info("Total processing of playlist time: " + total.Elapsed);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _log.Debug("Refreshing page");
            RefreshPage();
            _log.Debug("Page refreshed");
        }

        public void RefreshPage()
        {
            // Map Playlist IDs to playlist names
            var map = new Dictionary<string, string>();
            try
            {
                foreach (string item in Properties.Settings.Default.PlaylistIDs)
                {
                    map.Add(GetPlaylistName.GetPlaylistNameAsync(item).Result, item);
                }
            }
            catch (Exception e)
            {
                _log.Error("Error during retrieving playlist names." + e.Message);
            }

            comboboxPlaylistIDs.ItemsSource = map;
            comboboxPlaylistIDs.SelectedIndex = 0;
        }

        private void btnUpdateAllPlaylists_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("NOT YET IMPLEMENTED", "ERROR");
        }

        private void btnGetVideoUrlFile_Click(object sender, RoutedEventArgs e)
        {
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

            // Set variables
            playListId = comboboxPlaylistIDs.SelectedValue.ToString().Split(',')[1];
            playListId = playListId.Replace("]", "");

            // Get all video IDs from the YouTube playlist
            dynamic allVideoIds;
            try
            {
                allVideoIds = GetIDs.GetVideosInPlayListAsync(playListId).Result;
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
            btnUpdateSelectedPlaylistSlow.IsEnabled = false;
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
            btnUpdateSelectedPlaylistSlow.IsEnabled = true;
        }
        
    }
}
