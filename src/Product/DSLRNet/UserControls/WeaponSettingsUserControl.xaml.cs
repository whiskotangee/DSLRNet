namespace DSLRNet.UserControls;

using DSLRNet.Models;
using System.Windows.Controls;

/// <summary>
/// Interaction logic for WeaponSettingsUserControl.xaml
/// </summary>
public partial class WeaponSettingsUserControl : UserControl
{
    public WeaponSettingsUserControl()
    {
        InitializeComponent();
    }

    private void UniqueColor_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (this.DataContext is not WeaponGeneratorSettingsWrapper settings)
        {
            return;
        }       

        var colorDialog = new ColorDialog();
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            settings.UniqueItemNameColor = $"{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}";
        }
    }
}
