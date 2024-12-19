using DSLRNet.Models;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DSLRNet.UserControls
{
    /// <summary>
    /// Interaction logic for GeneralSettingsUserControl.xaml
    /// </summary>
    public partial class GeneralSettingsUserControl : UserControl
    {
        public GeneralSettingsUserControl()
        {
            InitializeComponent();
        }

        private void BrowseDeployPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                Title = "Select Mod Folder",
                FileName = (this.DataContext as SettingsWrapper).DeployPath,
                Filter = "Folders|no.files",
                ValidateNames = false
            };
            if (dialog.ShowDialog() == true)
            {
                ((SettingsWrapper)DataContext).DeployPath = System.IO.Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void BrowseGamePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select Elden Ring Game Exe",
                FileName = (this.DataContext as SettingsWrapper).GamePath,
                Filter = "*.exe",
                ValidateNames = false
            };

            if (dialog.ShowDialog() == true)
            {
                ((SettingsWrapper)DataContext).GamePath = System.IO.Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            return int.TryParse(text, out _);
        }
    }
}
