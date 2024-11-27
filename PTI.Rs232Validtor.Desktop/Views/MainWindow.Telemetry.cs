using PTI.Rs232Validator.Models;
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
        
        var wasSuccessful = await billValidator.PingAsync();
        DoOnUiThread(() => PingDisplay.ResultValue = wasSuccessful.ToString());
    }

    private async void GetSerialNumberDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var serialNumber = await billValidator.GetSerialNumberAsync();
        string resultValue;
        if (!string.IsNullOrEmpty(serialNumber))
        {
            resultValue = serialNumber;
        }
        else if (serialNumber is not null && serialNumber.Length == 0)
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

        var cashboxMetrics = await billValidator.GetCashboxMetrics();
        var resultValue = cashboxMetrics is null ? ErrorMessage : cashboxMetrics.ToString();
        DoOnUiThread(() => GetCashboxMetricsDisplay.ResultValue = resultValue);
    }

    private async void ClearCashboxCountDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var wasSuccessful = await billValidator.ClearCashboxCount();
        DoOnUiThread(() => ClearCashboxCountDisplay.ResultValue = wasSuccessful.ToString());
    }

    private async void GetUnitMetricsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var unitMetrics = await billValidator.GetUnitMetrics();
        var resultValue = unitMetrics is null ? ErrorMessage : unitMetrics.ToString();
        DoOnUiThread(() => GetUnitMetricsDisplay.ResultValue = resultValue);
    }

    private async void GetServiceUsageCountersDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var serviceUsageCounters = await billValidator.GetServiceUsageCounters();
        var resultValue = serviceUsageCounters is null ? ErrorMessage : serviceUsageCounters.ToString();
        DoOnUiThread(() => GetServiceUsageCountersDisplay.ResultValue = resultValue);
    }

    private async void GetServiceFlagsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var serviceFlags = await billValidator.GetServiceFlags();
        var resultValue = serviceFlags is null ? ErrorMessage : serviceFlags.ToString();
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

        var wasSuccessful = await billValidator.ClearServiceFlags(correctableComponent);
        DoOnUiThread(() => ClearServiceFlagsDisplay.ResultValue = wasSuccessful.ToString());
    }

    private async void GetServiceInfoDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var serviceInfo = await billValidator.GetServiceInfo();
        var resultValue = serviceInfo is null ? ErrorMessage : serviceInfo.ToString();
        DoOnUiThread(() => GetServiceInfoDisplay.ResultValue = resultValue);
    }

    private async void GetFirmwareMetricsDisplay_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        var firmwareMetrics = await billValidator.GetFirmwareMetrics();
        var resultValue = firmwareMetrics is null ? ErrorMessage : firmwareMetrics.ToString();
        DoOnUiThread(() => GetFirmwareMetricsDisplay.ResultValue = resultValue);
    }
}