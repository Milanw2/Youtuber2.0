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
using Telegram.Bot;
using Telegram.Bot.Args;

namespace Youtuber2._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: Future feature list :
        // Add logic to update all the playlist with btnUpdateAllPlaylists_Click
        // Add logging (configurable with all or only ERROR) to the UI
        // Add counter (sort of progressbar maybe?)

        // Global variables
        string playListId;
        string pathMp3Files;
        string pathMp3Folder;
        string playlistTitle;
        string selectedPlaylistID;
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
            api.init();
            _log.Info("Application opened");
            API.SendErrors = SendErrors.IsChecked.Value;
            API.SendInfo = SendInfo.IsChecked.Value;
        }
        private void InitVariables()
        {
            playListId = "";
            pathMp3Files = "";
            pathMp3Folder = "";
            playlistTitle = "";
            selectedPlaylistID = "";
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
                _log.Error(ex.InnerException.Message + "There is probably something wrong with your playlist ID. See manual for more information");
                api.TelegramBotSendError(ex.InnerException.Message + "There is probably something wrong with your playlist ID. See manual for more information");
            }
            
            label_amount.Content = listbox_logging.Items.Count.ToString();
        }
        private async void btnUpdateSelectedPlaylist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Stopwatch total = new Stopwatch();
                total.Start();

                disableButtons();
                await Task.Run(() => this.UpdateSelectedPlaylist(sender, e));
                enableButtons();

                total.Stop();
                api.TelegramBotSendInfo("Processed playlist in " + total.Elapsed + " seconds.");
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                api.TelegramBotSendError(ex.ToString());
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

                api.TelegramBotSendInfo("Processing: " + playlistTitle);

                // Get all video IDs from the YouTube playlist
                allVideoIds = GetIDs.GetVideosInPlayListAsync(playListId).Result;

                // Check if IDs where found
                if (allVideoIds.Count == 0)
                {
                    _log.Error("Video IDs where retrieved but empty -> probably empty playlist");
                    api.TelegramBotSendError("Video IDs where retrieved but empty -> probably empty playlist");
                    MessageBox.Show("Empty playlist?");
                    return;
                }
                _log.Debug("Retrieval is starting...");

                api.ProcessVideosToMp3(allVideoIds, pathMp3Files, playlistTitle);

                // Reset variables just to be sure
                InitVariables();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal error, send file 'proper.log' to me!");
                api.TelegramBotSendError("Fatal error, send file 'proper.log' to me!");
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
            pathMp3Files = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle + "\\");
            _log.Debug("Mp3 file path = " + pathMp3Files);
            pathMp3Folder = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.FilePath + "\\Youtuber\\Mp3\\" + playlistTitle);
        }
        private bool DefaultChecks()
        {
            // check if a playlist is selected
            if (selectedPlaylistID == null)
            {
                _log.Error("No playlist available to download or no playlist selected (impossible)");
                api.TelegramBotSendError("No playlist available to download or no playlist selected (impossible)");
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
                api.TelegramBotSendError("Error during retrieving playlist names." + e.Message);
            }

            comboboxPlaylistIDs.ItemsSource = map;
            comboboxPlaylistIDs.SelectedIndex = 0;
        }
        private async void btnUpdateAllPlaylists_Click(object sender, RoutedEventArgs e)
        {
            int errorCount = 0;
            Stopwatch total = new Stopwatch();
            total.Start();
            disableButtons();

            this.Dispatcher.Invoke(() =>
            {
                selectedPlaylistID = comboboxPlaylistIDs.SelectedValue.ToString();
            });

            foreach (string playlistId in comboboxPlaylistIDs.ItemsSource)
            {
                try
                {
                    await Task.Run(() => this.UpdateSelectedPlaylist(sender, e));
                }
                catch (Exception ex)
                {
                    _log.Error(ex.ToString());
                    api.TelegramBotSendError(ex.ToString());
                    errorCount++;
                }
            }

            enableButtons();
            total.Stop();
            api.TelegramBotSendInfo("Processed all playlists in " + total.Elapsed + " seconds. Playlists in error count : " + errorCount);
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
                api.TelegramBotSendError("No playlist available to download or no playlist selected (impossible)");
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
                api.TelegramBotSendError("Video IDs where retrieved but empty -> probably empty playlist");
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
        private void SendErrors_Checked(object sender, RoutedEventArgs e)
        {
            API.SendErrors = SendErrors.IsChecked.Value;
        }
         private void SendInfo_Checked(object sender, RoutedEventArgs e)
        {
            API.SendInfo = SendInfo.IsChecked.Value;
        }
    }
}
