using PTI.Rs232Validator.BillValidators;
using PTI.Rs232Validator.SerialProviders;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion implements INotifyPropertyChanged and establishes a connection to an acceptor.
/// <summary>
/// Main window of application.
/// </summary>
public partial class MainWindow : INotifyPropertyChanged
{
    private const string ErrorMessage = "An error occurred.";

    private readonly string _selectPortText;
    private readonly string _startPollingText;
    private readonly string _stopPollingText;
    private readonly string _stateTagText;
    private readonly string _eventTagText;
    private readonly Rs232Configuration? _rs232Configuration;
    
    private BillValidator? _billValidator;
    private bool _isPolling;

    public MainWindow()
    {
        InitializeComponent();

        Title = "RS-232 GUI";
        var version = typeof(BillValidator).Assembly.GetName().Version;
        if (version is not null)
        {
            Title += $" v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        _selectPortText = FindResource("SelectPortText") as string ??
                          throw new InvalidOperationException("String resource with key 'SelectPortText' not found.");
        _startPollingText = FindResource("StartPollingText") as string ??
                            throw new InvalidOperationException(
                                "String resource with key 'StartPollingText' not found.");
        _stopPollingText = FindResource("StopPollingText") as string ??
                           throw new InvalidOperationException("String resource with key 'StopPollingText' not found.");
        _stateTagText = FindResource("StateTagText") as string ??
                        throw new InvalidOperationException("String resource with key 'StateTagText' not found.");
        _eventTagText = FindResource("EventTagText") as string ??
                        throw new InvalidOperationException("String resource with key 'EventTagText' not found.");

        _rs232Configuration = new Rs232Configuration
        {
            EnableMask = GetEnableMask(),
            ShouldEscrow = IsInEscrowMode,
            ShouldDetectBarcodes = IsBarcodeDetectionEnabled,
            PollingPeriod = TimeSpan.FromMilliseconds(uint.Parse(PollRateTextBox.Text))
        };

        // Visit MainWindow.Logger for more information.
        SetUpLogAutoScroll();
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Is the acceptor actively being polled?
    /// </summary>
    public bool IsPolling
    {
        get => _isPolling;
        set
        {
            DoOnUiThread(() => { PollButton.Content = value ? _stopPollingText : _startPollingText; });

            _isPolling = value;
            NotifyPropertyChanged(nameof(IsPolling));
        }
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void DoOnUiThread(Action action)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(action);
        }
        else
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// Loads the available ports into the combo box.
    /// </summary>
    private void AvailablePortsComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        AvailablePortsComboBox.ItemsSource = SerialPort.GetPortNames();
    }

    /// <inheritdoc cref="AvailablePortsComboBox_Loaded"/>
    private void AvailablePortsComboBox_MouseLeave(object sender, MouseEventArgs e)
    {
        AvailablePortsComboBox.ItemsSource = SerialPort.GetPortNames();
    }

    /// <summary>
    /// Creates a new instance of <see cref="_billValidator"/> and assigns event handlers.
    /// </summary>
    private void AvailablePortsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsPolling)
        {
            return;
        }

        _billValidator?.Dispose();
        _billValidator = null;

        if (_rs232Configuration is null || e.AddedItems.Count == 0 || e.AddedItems[0] is not string)
        {
            return;
        }

        var portName = e.AddedItems[0] as string;
        if (string.IsNullOrEmpty(portName) || portName == _selectPortText)
        {
            return;
        }

        var serialPortProvider = SerialProvider.CreateUsbSerialProvider(this, portName);
        _billValidator = new BillValidator(this, serialPortProvider, _rs232Configuration);

        _billValidator.OnConnectionLost += BillValidator_OnConnectionLost;
        
        // Visit MainWindow.Logger.cs for more information.
        _billValidator.OnCommunicationAttempted += BillValidator_OnCommunicationAttempted;

        // Visit MainWindow.StatesAndEvents.cs for more information.
        _billValidator.OnStateChanged += BillValidator_OnStateChanged;
        _billValidator.OnEventReported += BillValidator_OnEventReported;
        _billValidator.OnCashboxAttached += BillValidator_CashboxAttached;
        _billValidator.OnCashboxRemoved += BillValidator_CashboxRemoved;

        // Visit MainWindow.Escrow.cs for more information.
        _billValidator.OnBillEscrowed += BillValidator_OnBillEscrowed;

        // Visit MainWindow.Bank for more information.
        _billValidator.OnBillStacked += BillValidator_OnBillStacked;

        // Visit MainWindow.Extended for more information.
        _billValidator.OnBarcodeDetected += BillValidator_OnBarcodeDetected;
    }

    /// <summary>
    /// Gets <see cref="_billValidator"/> if it is not null; otherwise, shows a message box.
    /// </summary>
    private BillValidator? GetBillValidatorOrShowMessage()
    {
        if (_billValidator is not null)
        {
            return _billValidator;
        }

        MessageBox.Show("Please select a port.");
        return null;
    }

    /// <summary>
    /// Starts or stops polling the acceptor.
    /// </summary>
    private void PollButton_Click(object sender, RoutedEventArgs e)
    {
        var billValidator = GetBillValidatorOrShowMessage();
        if (billValidator is null)
        {
            return;
        }

        billValidator.StopPollingLoop();

        if (IsPolling)
        {
            IsPolling = false;
            return;
        }

        if (PollRateTextBox.Text.Length > 4 || !ushort.TryParse(PollRateTextBox.Text, out var ms) || ms < 100)
        {
            MessageBox.Show("Enter a poll rate between 100 and 9999.");
            return;
        }

        billValidator.Configuration.PollingPeriod = TimeSpan.FromMilliseconds(ms);
        IsPolling = billValidator.StartPollingLoop();
        if (!IsPolling)
        {
            billValidator.Dispose();
            MessageBox.Show("Failed to connect to the acceptor.");
        }
    }

    private void BillValidator_OnConnectionLost(object? sender, EventArgs e)
    {
        LogInfo("Lost connection to the acceptor.");
        _billValidator?.StopPollingLoop();
        IsPolling = false;
    }

    /// <summary>
    /// Prevents non-numeric input in <see cref="PollRateTextBox"/>.
    /// </summary>
    private void PollRateTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !uint.TryParse(e.Text, out _);
    }
}