namespace DSLRNet;
using ControlzEx.Theming;
using DSLRNet.ViewModels;
using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;

using System.Collections.Specialized;
using System.Windows;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            MainWindowViewModel viewModel = new();

            MainWindow mainWindow = new(viewModel);
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while starting the app: {ex}", "Error During Startup", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString(), "Unhandled Exception - Something has gone very wrong");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString(), "Unhandled Asynchronous Exception");
    }
}