using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PTI.Rs232Validator.Gui.Views;

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

    private async void GetValueTable_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }
        
        var responseMessage = await billValidator.GetRequestValueTable();
        if (responseMessage is { IsValid: true, ValueTable.Length: > 0})
        {
            var resultDataGrid = ConvertToDataGrid(responseMessage.ValueTable);
            DoOnUiThread(() => ValueTableContainer.Content = resultDataGrid);
        }
        else
        {
            DoOnUiThread(() => ValueTableContainer.Content = new TextBlock { Text = ErrorMessage });
        }
    }

    private DataGrid ConvertToDataGrid(string responseMessageValueTable)
    {
        
        List<CurrencyGridRow> rows = ParseRows(responseMessageValueTable);

        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            CanUserReorderColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            ItemsSource = rows,
        };

        dataGrid.Columns.Add(
            new DataGridTextColumn
            {
                Header = "IDX",
                Binding = new Binding(nameof(CurrencyGridRow.Index)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        
        dataGrid.Columns.Add(
            new DataGridTextColumn
            {
                Header = "ISO",
                Binding = new Binding(nameof(CurrencyGridRow.IsoCode)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        
        dataGrid.Columns.Add(
            new DataGridTextColumn
            {
                Header = "VALUE",
                Binding = new Binding(nameof(CurrencyGridRow.Value)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        
        return dataGrid;
    }

    private List<CurrencyGridRow> ParseRows(string valueTableString)
    {
        var rows = valueTableString.Split(new[] { '|' }, StringSplitOptions.None);

        var result = new List<CurrencyGridRow>();
        
        foreach (var row in rows)
        {
            var fields = row.Split(new[] { ',' }, StringSplitOptions.None);
            
            result.Add(
                new CurrencyGridRow
                {
                    Index = fields[0],
                    IsoCode = fields[1],
                    Value = fields[2],
                });
        }
        
        return result;
    }
}

public sealed class CurrencyGridRow
{
    public string Index { get; set; }
    public string IsoCode { get; set; }
    public string Value { get; set; }
}