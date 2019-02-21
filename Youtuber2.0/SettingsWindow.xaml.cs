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
using System.Windows.Shapes;
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
            this.updatePlaylistIDs();
            this.btnDelete.Visibility = Visibility.Hidden;
            txtblockFilepath.Text = Properties.Settings.Default.FilePath.ToString();
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.PlaylistIDs.Add(txtboxPlaylistID.Text);
            this.updatePlaylistIDs();
            Properties.Settings.Default.Save();
        }

        private void listboxPlaylistIDs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnDelete.Visibility = Visibility.Visible;
            if (listboxPlaylistIDs.SelectedItem != null)
            {
                labelTitle.Content = GetPlaylistName.GetPlaylistNameAsync(listboxPlaylistIDs.SelectedItem.ToString()).Result;
            }
        }

        public void btnDeleteClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.PlaylistIDs.RemoveAt(listboxPlaylistIDs.SelectedIndex);
            Properties.Settings.Default.Save();
            this.updatePlaylistIDs();
        }

        public void updatePlaylistIDs()
        {
            listboxPlaylistIDs.ItemsSource =
                Properties.Settings.Default.PlaylistIDs.Cast<string>().ToArray();
            listboxPlaylistIDs.SelectedIndex = -1;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnFolderBrowser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            WinForms.DialogResult result = dialog.ShowDialog();
            Properties.Settings.Default.FilePath = dialog.SelectedPath;
            Properties.Settings.Default.Save();
            txtblockFilepath.Text = Properties.Settings.Default.FilePath.ToString();
        }
    }
}
