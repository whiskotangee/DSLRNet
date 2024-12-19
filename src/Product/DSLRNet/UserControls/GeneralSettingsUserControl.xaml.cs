using DSLRNet.Core.Config;
using DSLRNet.Core.Handlers;
using DSLRNet.Models;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tomlyn.Model;
using Tomlyn;
using System.IO;

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
            var settingsWrapper = this.DataContext as SettingsWrapper;

            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                Title = "Select Mod Folder",
                FileName = settingsWrapper.DeployPath,
                DefaultDirectory = settingsWrapper.DeployPath,
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
            var settingsWrapper = this.DataContext as SettingsWrapper;

            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select Elden Ring Game Exe",
                FileName = settingsWrapper.GamePath,
                DefaultDirectory = settingsWrapper.GamePath,
                Filter = "*.exe",
                ValidateNames = false
            };

            if (dialog.ShowDialog() == true)
            {
                ((SettingsWrapper)DataContext).GamePath = System.IO.Path.GetDirectoryName(dialog.FileName);
            }
        }

        private void ParseToml_Click(object sender, RoutedEventArgs e)
        {
            var settingsWrapper = this.DataContext as SettingsWrapper;

            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Select mod engine toml file",
                FileName = "Choose toml file",
                DefaultDirectory = settingsWrapper.DeployPath,
                Filter = "*.toml",
                ValidateNames = false
            };

            if (dialog.ShowDialog() == true)
            {
                ((SettingsWrapper)DataContext).GamePath = System.IO.Path.GetDirectoryName(dialog.FileName);
            }

            ParseModDirectoriesFromToml(settingsWrapper, dialog.FileName);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            return int.TryParse(text, out _);
        }

        private void ParseModDirectoriesFromToml(SettingsWrapper settings, string fullPath)
        {
            var tomlContent = File.ReadAllText(fullPath);
            var table = Toml.Parse(tomlContent).ToModel();

            settings.ModPaths.Clear();

            var modPaths = new List<string>();

            if (table.TryGetValue("extension.mod_loader", out var modLoaderSection))
            {
                var modLoaderTable = modLoaderSection as TomlTable;
                if (modLoaderTable != null && modLoaderTable.TryGetValue("mods", out var mods))
                {
                    var modsArray = mods as TomlArray;
                    if (modsArray != null)
                    {
                        foreach (var mod in modsArray.OfType<TomlTable>())
                        {
                            if (mod.TryGetValue("enabled", out var enabled)
                                && (bool)enabled
                                && mod.TryGetValue("path", out var path))
                            {
                                settings.ModPaths.Add(Path.Combine(Path.GetDirectoryName(fullPath), path.ToString()));
                            }
                        }
                    }
                }
            }
        }
    }
}
