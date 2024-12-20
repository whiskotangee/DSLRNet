namespace DSLRNet;

using DSLRNet.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls.Primitives;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
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

    private void OutputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ((TextBoxBase)sender).ScrollToEnd();
    }
}