using DSLRNet.Models;

using System.Windows;
using System.Windows.Input;
using Tomlyn.Model;
using Tomlyn;
using System.IO;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace DSLRNet.UserControls
{
    /// <summary>
    /// Interaction logic for GeneralSettingsUserControl.xaml
    /// </summary>
    public partial class GeneralSettingsUserControl : System.Windows.Controls.UserControl
    {
        public GeneralSettingsUserControl()
        {
            InitializeComponent();
        }

        private void BrowseDeployPath_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SettingsWrapper settingsWrapper)
            {
                using FolderBrowserDialog folderChooser = new()
                {
                    Description = "Select Deploy Folder",
                    SelectedPath = settingsWrapper.DeployPath,
                    InitialDirectory = settingsWrapper.DeployPath
                };

                if (folderChooser.ShowDialog() == DialogResult.OK)
                {
                    ((SettingsWrapper)DataContext).DeployPath = folderChooser.SelectedPath;
                    if (settingsWrapper.ModPaths.Count > 0)
                    {
                        settingsWrapper.ModPaths.Remove(Path.Combine(Path.GetDirectoryName(settingsWrapper.DeployPath) ?? string.Empty, folderChooser.SelectedPath.ToString() ?? string.Empty));
                    }
                }
            }
        }

        private void BrowseGamePath_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SettingsWrapper settingsWrapper)
            {
                var dialog = new OpenFileDialog
                {
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Title = "Select Elden Ring Game Exe",
                    FileName = "eldenring.exe",
                    InitialDirectory = settingsWrapper.GamePath,
                    Filter = "Elden Ring Executable (eldenring.exe)|eldenring.exe",
                    ValidateNames = true
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    settingsWrapper.GamePath = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                }
            }
        }

        private void ParseToml_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SettingsWrapper settingsWrapper)
            {
                var dialog = new OpenFileDialog
                {
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Title = "Select mod engine 2 toml file",
                    FileName = "Choose toml file",
                    InitialDirectory = settingsWrapper.DeployPath,
                    Filter = "TOML files (*.toml)|*.toml",
                    ValidateNames = false
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ParseModDirectoriesFromToml(settingsWrapper, dialog.FileName);
                }
            }
        }

        private void ClearModDirectories_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SettingsWrapper settingsWrapper)
            {
                settingsWrapper.ModPaths.Clear();
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

        private void ParseModDirectoriesFromToml(SettingsWrapper settings, string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return;
            }

            var tomlContent = File.ReadAllText(fullPath);
            var table = Toml.Parse(tomlContent).ToModel();

            settings.ModPaths.Clear();

            if (!table.TryGetValue("extension", out var extensionsSection))
            {
                return;
            }

            if (extensionsSection is not TomlTable extensionsTable || !extensionsTable.TryGetValue("mod_loader", out var modLoader))
            {
                return;
            }

            if (modLoader is not TomlTable modLoaderTable || !modLoaderTable.TryGetValue("mods", out var mods))
            {
                return;
            }

            if (mods is not TomlArray modArray)
            {
                return;
            }

            foreach (var mod in modArray.OfType<TomlTable>())
            {
                if (mod.TryGetValue("enabled", out var enabled)
                    && (bool)enabled
                    && mod.TryGetValue("path", out var path)
                    && !string.Equals(settings.DeployPath, Path.Combine(Path.GetDirectoryName(fullPath) ?? string.Empty, path.ToString() ?? string.Empty), StringComparison.OrdinalIgnoreCase))
                {
                    settings.ModPaths.Add(Path.Combine(Path.GetDirectoryName(fullPath) ?? string.Empty, path.ToString() ?? string.Empty));
                }
            }
        }
    }
}
