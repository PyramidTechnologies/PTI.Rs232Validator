using System.Windows;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion communicates with an acceptor via extended commands.
public partial class MainWindow
{
    private void BillValidator_OnBarcodeDetected(object? sender, string barcode)
    {
        LogInfo("Detected barcode: {0}.", barcode);
        DoOnUiThread(() => GetDetectedBarcodeDisplay.ResultValue = barcode);
    }

    private async void GetDetectedBarcodeDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        if (BillValidator is null)
        {
            return;
        }

        var barcode = await BillValidator.GetDetectedBarcode();
        string resultValue;
        if (!string.IsNullOrEmpty(barcode))
        {
            resultValue = barcode;
        }
        else if (barcode is not null && barcode.Length == 0)
        {
            resultValue = "No barcode was detected.";
        }
        else
        {
            resultValue = ErrorMessage;
        }
        
        DoOnUiThread(() => GetDetectedBarcodeDisplay.ResultValue = resultValue);
    }
}