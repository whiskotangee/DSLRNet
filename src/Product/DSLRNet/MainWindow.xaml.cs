namespace DSLRNet;

using DSLRNet.ViewModels;
using MahApps.Metro.Controls;
using System.Collections.Specialized;
using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        this.DataContext = viewModel;

        viewModel.LogMessages.CollectionChanged += LogMessages_CollectionChanged;
    }

    private void LogMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            LogScrollViewer.ScrollToEnd();
        }
    }

    private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}