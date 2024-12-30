namespace DSLRNet.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

public class BaseModel<T> 
    : INotifyPropertyChanged where T : class
{
    public T? OriginalObject { get; protected set; } = default;

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
