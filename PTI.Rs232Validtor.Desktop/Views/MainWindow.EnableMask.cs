using System.Windows;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion mutates the enable mask of the RS-232 configuration.
public partial class MainWindow
{
    private byte GetEnableMask()
    {
        var enableMask = 0;
        enableMask |= EnableMaskCheckBox1?.IsChecked is not null && EnableMaskCheckBox1.IsChecked.Value ? 1 << 0 : 0;
        enableMask |= EnableMaskCheckBox2?.IsChecked is not null && EnableMaskCheckBox2.IsChecked.Value ? 1 << 1 : 0;
        enableMask |= EnableMaskCheckBox3?.IsChecked is not null && EnableMaskCheckBox3.IsChecked.Value ? 1 << 2 : 0;
        enableMask |= EnableMaskCheckBox4?.IsChecked is not null && EnableMaskCheckBox4.IsChecked.Value ? 1 << 3 : 0;
        enableMask |= EnableMaskCheckBox5?.IsChecked is not null && EnableMaskCheckBox5.IsChecked.Value ? 1 << 4 : 0;
        enableMask |= EnableMaskCheckBox6?.IsChecked is not null && EnableMaskCheckBox6.IsChecked.Value ? 1 << 5 : 0;
        enableMask |= EnableMaskCheckBox7?.IsChecked is not null && EnableMaskCheckBox7.IsChecked.Value ? 1 << 6 : 0;
        return (byte)enableMask;
    }
    
    /// <summary>
    /// Mutates <see cref="Rs232Configuration.EnableMask"/> for <see cref="_billValidator"/>.
    /// </summary>
    private void EnableMaskCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (_rs232Configuration is null)
        {
            return;
        }
        
        _rs232Configuration.EnableMask = GetEnableMask();
    }
}