namespace DSLRNet.ViewModels;

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.Input;
using DSLRNet.Core.Config;
using DSLRNet.Core;
using DSLRNet.Models;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using ImageMagick;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public static readonly object lockObject = new();

    private SettingsWrapper settingsWrapper;
    private ThreadSafeObservableCollection<string> logMessages;
    private OperationProgressTracker progressTracker;
    private bool isRunning;
    private bool hasRun;
    private int selectedTabIndex;
    private string lastRunCompleteMessage = string.Empty;
    private Brush lastRunCompleteColor = new SolidColorBrush(Colors.Black);

    public MainWindowViewModel()
    {
        this.settingsWrapper = new SettingsWrapper(Core.Config.Settings.CreateFromSettingsIni() ?? new Settings());
        GenerateLootCommand = new AsyncRelayCommand(GenerateLootAsync, () => !IsRunning);
        ChangeImageCommand = new RelayCommand<object?>(ChangeImage);
        OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
        PickUniqueNameColorCommand = new RelayCommand(PickUniqueNameColor);
        EditSettingsCommand = new RelayCommand(EditSettings);
        progressTracker = new OperationProgressTracker();
        IsRunning = false;
        logMessages = [];

        BindingOperations.EnableCollectionSynchronization(LogMessages, lockObject);
    }

    private void EditSettings()
    {
        LogMessages.Add($"Saving current config to Settings.User.ini");
        settingsWrapper.OriginalObject.ValidatePaths();

        if (settingsWrapper.RandomSeed == 0)
        {
            settingsWrapper.RandomSeed = new Random().Next();
        }

        settingsWrapper.OriginalObject.SaveSettings("Settings.User.ini");

        Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = "Settings.User.ini"
        });
    }

    private void PickUniqueNameColor()
    {

    }

    private async Task GenerateLootAsync()
    {
        // Your logic to generate loot goes here
        HasRun = true;
        IsRunning = true;
        bool success = false;

        try
        {
            ProgressTracker.Reset();
            LogMessages.Clear();
            if (settingsWrapper.OriginalObject == null)
            {
                throw new InvalidOperationException("Settings detected as null, cannot build");
            }

            LogMessages.Add($"Saving current config to Settings.User.ini");
            settingsWrapper.OriginalObject.ValidatePaths();
            if (settingsWrapper.RandomSeed == 0)
            {
                settingsWrapper.RandomSeed = new Random().Next();
            }

            settingsWrapper.OriginalObject.SaveSettings("Settings.User.ini");

            LogMessages.Add("Starting loot generation...");
            
            await Task.Run(async () =>
            {
                await Task.Yield();

                try
                {
                    await DSLRRunner.Run(settingsWrapper.OriginalObject, LogMessages, ProgressTracker);
                    LogMessages.Add("Loot generation completed successfully.");
                    success = true;
                }
                catch (Exception ex)
                {
                    LogMessages.Add($"Exception caught - loot Generation is most likely incomplete: {ex}");
                }
            });

        }
        catch (Exception ex)
        {
            LogMessages.Add($"Exception caught, cannot generate loot: {ex}");
            success = false;
        }
        finally
        {
            IsRunning = false;
            if (success)
            {
                LastRunCompleteMessage = "Success!";
                LastRunCompleteColor = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                LastRunCompleteMessage = "Last Run Failed - Check Logs";
                LastRunCompleteColor = new SolidColorBrush(Colors.Crimson);
            }
        }
    }

    private void ChangeImage(object? item)
    {
        if (item == null)
            return;

        var openFileDialog = new OpenFileDialog
        {
            Filter = "PNG Files (*.png)|*.png"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var selectedFilePath = openFileDialog.FileName;
            var fileName = Path.GetFileName(selectedFilePath);
            var destinationPath = Path.Combine("Assets", "LootIcons", fileName);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.Combine("Assets", "LootIcons"));

            using var imageTest = new MagickImage(selectedFilePath);
            if (imageTest.Width != imageTest.Height)
            {
                MessageBox.Show("Image must be 1024x1024", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Copy the file to the destination
            File.Copy(selectedFilePath, destinationPath, true);

            // Update the BackgroundImageName property
            ((RarityIconDetailsWrapper)item).BackgroundImageName = destinationPath;
        }
    }

    private void OpenLogFolder()
    {
        string logFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        if (Directory.Exists(logFolderPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = logFolderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            MessageBox.Show("Log folder does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    public IAsyncRelayCommand GenerateLootCommand { get; private set; }

    public ICommand EditSettingsCommand { get; private set; }

    public ICommand ChangeImageCommand { get; private set; }

    public ICommand OpenLogFolderCommand { get; private set; }

    public ICommand PickUniqueNameColorCommand { get; private set; }


    public bool IsRunning
    {
        get => isRunning;
        set
        {
            isRunning = value;
            OnPropertyChanged();
            SelectedTabIndex = value ? 3 : hasRun ? 3 : 0;
        }
    }

    public bool HasRun
    {
        get => hasRun;
        set
        {
            hasRun = value;
            OnPropertyChanged();
        }
    }

    public string LastRunCompleteMessage 
    { 
        get => lastRunCompleteMessage; 
        set
        {
            if (lastRunCompleteMessage != value)
            {
                lastRunCompleteMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public Brush LastRunCompleteColor
    {
        get => lastRunCompleteColor;
        set
        {
            if (lastRunCompleteColor != value)
            {
                lastRunCompleteColor = value;
                OnPropertyChanged();
            }
        }
    }
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set
        {
            if (selectedTabIndex != value)
            {
                selectedTabIndex = value;
                OnPropertyChanged();
            }
        }
    }

    public SettingsWrapper Settings
    {
        get => settingsWrapper;
        set
        {
            settingsWrapper = value;
            OnPropertyChanged();
        }
    }

    public ThreadSafeObservableCollection<string> LogMessages
    {
        get => this.logMessages;
        set
        {
            this.logMessages = value;
            OnPropertyChanged();
        }
    }

    public OperationProgressTracker ProgressTracker
    {
        get => this.progressTracker;
        set
        {
            this.progressTracker = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
