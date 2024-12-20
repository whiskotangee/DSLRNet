namespace DSLRNet.ViewModels;

using CommunityToolkit.Mvvm.Input;
using DSLRNet.Core;
using DSLRNet.Core.Config;
using DSLRNet.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public static readonly object lockObject = new object();

    public MainWindowViewModel()
    {
        this.settingsWrapper = new SettingsWrapper(Core.Config.Settings.CreateFromSettingsIni() ?? new Settings());
        GenerateLootCommand = new AsyncRelayCommand(GenerateLootAsync, () => !IsRunning);
        ProgressTracker = new OperationProgressTracker();
        IsRunning = false;
        LogMessages = [];

        BindingOperations.EnableCollectionSynchronization(LogMessages, lockObject);
    }

    private SettingsWrapper settingsWrapper;
    private ThreadSafeObservableCollection<string> logMessages;
    private OperationProgressTracker progressTracker;
    
    public IAsyncRelayCommand GenerateLootCommand { get; private set; }

    private bool isRunning;
    private bool hasRun;
    private int selectedTabIndex;

    private async Task GenerateLootAsync()
    {
        // Your logic to generate loot goes here
        HasRun = true;
        IsRunning = true;
        ProgressTracker.Reset();
        LogMessages.Clear();
        LogMessages.Add("Starting loot generation...");

        await Task.Run(async () =>
        {
            await Task.Yield();

            await DSLRRunner.Run(settingsWrapper.OriginalObject, LogMessages, ProgressTracker);
        });
        
        LogMessages.Add("Loot generation completed successfully.");
        IsRunning = false;
    }

    public bool IsRunning 
    { 
        get => isRunning;
        set
        {
            isRunning = value;
            OnPropertyChanged();
            SelectedTabIndex = value ? 3 : 0;
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

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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

public class UIBlockDetector
{
    static Timer _timer;
    public UIBlockDetector(int maxFreezeTimeInMilliseconds = 500)
    {
        var sw = new Stopwatch();

        new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Send, (sender, args) =>
        {
            lock (sw)
            {
                sw.Restart();
            }

        }, Application.Current.Dispatcher);

        _timer = new Timer(state =>
        {
            lock (sw)
            {
                if (sw.ElapsedMilliseconds > maxFreezeTimeInMilliseconds)
                {
                    Debugger.Break();
                    // Goto Visual Studio --> Debug --> Windows --> Theads 
                    // and checkup where the MainThread is.
                }
            }

        }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(10));

    }

}
