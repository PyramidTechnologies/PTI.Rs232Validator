using System.Windows;
using System.Windows.Input;

namespace PTI.Rs232Validator.Gui.Views;

public partial class MainWindow
{
    
    private async void RetransmitDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        
        if (!ushort.TryParse(RetransmitNumTextBox.Text, out var rn) || rn > 10 || rn < 1)
        {
            MessageBox.Show("Enter a number between 1 and 10.");
            return;
        }
        
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        billValidator.Configuration.RetransmissionNum = rn;
        
        var wasSuccessful = await billValidator.RetransmitLastMessageAsync();
        DoOnUiThread(() => RetransmitDisplay.ResultValue = wasSuccessful.ToString());
    }
    
    private void RetransmitNumTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !uint.TryParse(e.Text, out _);
    }
}