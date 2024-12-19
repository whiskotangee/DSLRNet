namespace DSLRNet.ViewModels;

using DSLRNet.Core.Config;
using DSLRNet.Models;
using System.ComponentModel;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public MainWindowViewModel()
    {
        this.settingsWrapper = new SettingsWrapper(Core.Config.Settings.CreateFromSettingsIni() ?? new Settings());
    }

    private SettingsWrapper settingsWrapper;

    public SettingsWrapper Settings
    {
        get => settingsWrapper;
        set 
        {
            settingsWrapper = value; 
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Settings)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
