using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;

namespace Youtuber2._0
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.UpdatePlaylistIDs();
            this.btnDelete.Visibility = Visibility.Hidden;
            //txtblockFilepath.Text = Properties.Settings.Default.FilePath.ToString();

            txtblockFilepath.Text = DBConnection.GetDestinationFolder();
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            //Properties.Settings.Default.PlaylistIDs.Add(txtboxPlaylistID.Text);
            string playlistId = GetPlaylistName.GetPlaylistNameAsync(txtboxPlaylistID.Text).Result;

            if (playlistId == "")
            {
                MessageBox.Show("No playlist found!", "Playlist code?");
            }
            else
            {
                DBConnection.InsertPlaylistIdById(playlistId, txtboxPlaylistID.Text);
            }
            

            this.UpdatePlaylistIDs();
            //Properties.Settings.Default.Save();
        }

        private void ListboxPlaylistIDs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnDelete.Visibility = Visibility.Visible;
            if (listboxPlaylistIDs.SelectedItem != null)
            {
                labelTitle.Content = GetPlaylistName.GetPlaylistNameAsync(listboxPlaylistIDs.SelectedItem.ToString()).Result;
            }
            if (labelTitle.Content == null)
            {
                MessageBox.Show("No playlist name found! Check private/public settings of playlist.", "Playlist name?");
            }
        }

        public void BtnDeleteClick(object sender, RoutedEventArgs e)
        {
            //Properties.Settings.Default.PlaylistIDs.RemoveAt(listboxPlaylistIDs.SelectedIndex);
            //Properties.Settings.Default.Save();
            if (listboxPlaylistIDs.SelectedValue != null)
            {
                DBConnection.DeletePlaylistIdById(listboxPlaylistIDs.SelectedValue.ToString());
            }
            
            this.UpdatePlaylistIDs();
        }

        public void UpdatePlaylistIDs()
        {
            //listboxPlaylistIDs.ItemsSource = Properties.Settings.Default.PlaylistIDs.Cast<string>().ToArray();
            listboxPlaylistIDs.ItemsSource = DBConnection.GetPlaylistIdsArray().ToArray();
            listboxPlaylistIDs.SelectedIndex = -1;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnFolderBrowser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            WinForms.DialogResult result = dialog.ShowDialog();
            //Properties.Settings.Default.FilePath = dialog.SelectedPath;
            //Properties.Settings.Default.Save();
            //txtblockFilepath.Text = Properties.Settings.Default.FilePath.ToString();

            DBConnection.SetDestinationFolder(dialog.SelectedPath);
            txtblockFilepath.Text = dialog.SelectedPath;
        }
    }
}
