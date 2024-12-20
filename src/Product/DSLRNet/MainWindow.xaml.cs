namespace DSLRNet;

using DSLRNet.ViewModels;
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
        this.DataContext = new MainWindowViewModel();
    }

    private void OutputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ((TextBoxBase)sender).ScrollToEnd();
    }
}