using System;
using System.Windows;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion communicates with an acceptor via telemetry commands.
public partial class MainWindow
{
    private async void PingDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var wasSuccessful = (await billValidator.PingAsync()).IsValid;
        DoOnUiThread(() => PingDisplay.ResultValue = wasSuccessful.ToString());
    }

    private async void GetSerialNumberDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetSerialNumberAsync();
        string resultValue;
        if (responseMessage is { IsValid: true, SerialNumber.Length: > 0 })
        {
            resultValue = responseMessage.SerialNumber;
        }
        else if (responseMessage is { IsValid: true, SerialNumber.Length: 0 })
        {
            resultValue = "The acceptor was not assigned a serial number.";
        }
        else
        {
            resultValue = ErrorMessage;
        }

        DoOnUiThread(() => GetSerialNumberDisplay.ResultValue = resultValue);
    }

    private async void GetCashboxMetricsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetCashboxMetrics();
        var resultValue = responseMessage.IsValid ? responseMessage.ToString() : ErrorMessage;
        DoOnUiThread(() => GetCashboxMetricsDisplay.ResultValue = resultValue);
    }

    private async void ClearCashboxCountDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var wasSuccessful = (await billValidator.ClearCashboxCount()).IsValid;
        DoOnUiThread(() => ClearCashboxCountDisplay.ResultValue = wasSuccessful.ToString());
    }

    private async void GetUnitMetricsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetUnitMetrics();
        var resultValue = responseMessage.IsValid ? responseMessage.ToString() : ErrorMessage;
        DoOnUiThread(() => GetUnitMetricsDisplay.ResultValue = resultValue);
    }

    private async void GetServiceUsageCountersDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetServiceUsageCounters();
        var resultValue = responseMessage.IsValid ? responseMessage.ToString() : ErrorMessage;
        DoOnUiThread(() => GetServiceUsageCountersDisplay.ResultValue = resultValue);
    }

    private async void GetServiceFlagsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetServiceFlags();
        var resultValue = responseMessage.IsValid ? responseMessage.ToString() : ErrorMessage;
        DoOnUiThread(() => GetServiceFlagsDisplay.ResultValue = resultValue);
    }

    private async void ClearServiceFlagsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var correctableComponentText = ComponentComboBox.Text;
        if (!Enum.TryParse<CorrectableComponent>(correctableComponentText, out var correctableComponent))
        {
            return;
        }

        var wasSuccessful = (await billValidator.ClearServiceFlags(correctableComponent)).IsValid;
        DoOnUiThread(() => ClearServiceFlagsDisplay.ResultValue = wasSuccessful.ToString());
    }

    private async void GetServiceInfoDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetServiceInfo();
        var resultValue = responseMessage.IsValid ? responseMessage.ToString() : ErrorMessage;
        DoOnUiThread(() => GetServiceInfoDisplay.ResultValue = resultValue);
    }

    private async void GetFirmwareMetricsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var responseMessage = await billValidator.GetFirmwareMetrics();
        var resultValue = responseMessage.IsValid ? responseMessage.ToString() : ErrorMessage;
        DoOnUiThread(() => GetFirmwareMetricsDisplay.ResultValue = resultValue);
    }
}