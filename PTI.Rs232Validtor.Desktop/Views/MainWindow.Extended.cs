using System.Windows;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion communicates with an acceptor via extended commands.
public partial class MainWindow
{
    private bool _isBarcodeDetectionEnabled;
    
    /// <summary>
    /// <see cref="Rs232Configuration.ShouldDetectBarcodes"/>.
    /// </summary>
    public bool IsBarcodeDetectionEnabled
    {
        get => _isBarcodeDetectionEnabled;
        set
        {
            if (_rs232Configuration is not null)
            {
                _rs232Configuration.ShouldDetectBarcodes = value;
            }
            
            _isBarcodeDetectionEnabled = value;
            NotifyPropertyChanged(nameof(IsBarcodeDetectionEnabled));
        }
    }
    
    private void BillValidator_OnBarcodeDetected(object? sender, string barcode)
    {
        LogInfo("Detected barcode: {0}.", barcode);
        DoOnUiThread(() => GetDetectedBarcodeDisplay.ResultValue = barcode);
    }

    private async void GetDetectedBarcodeDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetDetectedBarcode();
        string resultValue;
        if (responseMessage is { IsValid: true, Barcode.Length: > 0 })
        {
            resultValue = responseMessage.Barcode;
        }
        else if (responseMessage is { IsValid: true, Barcode.Length: 0 })
        {
            resultValue = "No barcode was detected since the last power cycle.";
        }
        else
        {
            resultValue = ErrorMessage;
        }
        
        DoOnUiThread(() => GetDetectedBarcodeDisplay.ResultValue = resultValue);
    }
}