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
    private readonly string _pauseText;
    private readonly string _resumeText;
    private readonly string _stateTagText;
    private readonly string _eventTagText;

    private string _portName = string.Empty;
    private bool _isPolling;

    public MainWindow()
    {
        InitializeComponent();

        _selectPortText = FindResource("SelectPortText") as string ??
                          throw new InvalidOperationException("String resource with key 'SelectPortText' not found.");
        _startPollingText = FindResource("StartPollingText") as string ??
                            throw new InvalidOperationException("String resource with key 'StartPollingText' not found.");
        _stopPollingText = FindResource("StopPollingText") as string ??
                           throw new InvalidOperationException("String resource with key 'StopPollingText' not found.");
        _pauseText = FindResource("PauseText") as string ??
                     throw new InvalidOperationException("String resource with key 'PauseText' not found.");
        _resumeText = FindResource("ResumeText") as string ??
                      throw new InvalidOperationException("String resource with key 'ResumeText' not found.");
        _stateTagText = FindResource("StateTagText") as string ??
                        throw new InvalidOperationException("String resource with key 'StateTagText' not found.");
        _eventTagText = FindResource("EventTagText") as string ??
                        throw new InvalidOperationException("String resource with key 'EventTagText' not found.");
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

    /// <summary>
    /// An instance of <see cref="BillValidator"/>.
    /// </summary>
    private BillValidator? BillValidator { get; set; }

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
    /// 
    /// </summary>
    private void AvailablePortsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsPolling)
        {
            return;
        }
        
        BillValidator?.Dispose();
        BillValidator = null;
        
        if (string.IsNullOrEmpty(AvailablePortsComboBox.Text) || AvailablePortsComboBox.Text == _selectPortText)
        {
            return;
        }
        
        _portName = AvailablePortsComboBox.Text;
        var serialPortProvider = SerialProvider.CreateUsbSerialProvider(this, _portName);

        var rs232Configuration = new Rs232Configuration();
        BillValidator = new BillValidator(this, serialPortProvider, rs232Configuration);

        BillValidator.OnConnectionLost += BillValidator_OnConnectionLost;

        // Visit MainWindow.StatesAndEvents.cs for more information.
        BillValidator.OnStateChanged += BillValidator_OnStateChanged;
        BillValidator.OnEventReported += BillValidator_OnEventReported;
        BillValidator.OnCashboxAttached += BillValidator_CashboxAttached;
        BillValidator.OnCashboxRemoved += BillValidator_CashboxRemoved;

        // Visit MainWindow.Escrow.cs for more information.
        IsInEscrowMode = true;
        BillValidator.OnBillEscrowed += BillValidator_OnBillEscrowed;

        // Visit MainWindow.Bank for more information.
        BillValidator.OnBillStacked += BillValidator_OnBillStacked;

        // Visit MainWindow.Extended for more information.
        BillValidator.OnBarcodeDetected += BillValidator_OnBarcodeDetected;
    }

    /// <summary>
    /// Starts or stops polling the acceptor.
    /// </summary>
    private void PollButton_Click(object sender, RoutedEventArgs e)
    {
        if (BillValidator is null)
        {
            MessageBox.Show("Please select a port.");
            return;
        }
        
        BillValidator.StopPollingLoop();

        if (IsPolling)
        {
            IsPolling = false;
            return;
        }
        
        // Start the RS-232 polling loop.
        IsPolling = BillValidator.StartPollingLoop();
        if (!IsPolling)
        {
            BillValidator?.Dispose();
            MessageBox.Show("Failed to connect to the acceptor.");
        }
    }

    private void BillValidator_OnConnectionLost(object? sender, EventArgs e)
    {
        LogInfo("Lost connection to the acceptor.");
        IsPolling = false;
    }

    /// <summary>
    /// Pauses or resumes the acceptance of bills.
    /// </summary>
    private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (BillValidator is null)
        {
            return;
        }

        if (BillValidator.CanAcceptBills)
        {
            BillValidator.ForbidBillAcceptance();
            PauseResumeButton.Content = _resumeText;
        }
        else
        {
            BillValidator.AllowBillAcceptance();
            PauseResumeButton.Content = _pauseText;
        }
    }

    /// <summary>
    /// Mutates <see cref="Rs232Configuration.PollingPeriod"/>.
    /// </summary>
    private void PollSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BillValidator is null)
        {
            return;
        }

        var ms = (int)e.NewValue;
        BillValidator.Configuration.PollingPeriod = TimeSpan.FromMilliseconds(ms);
        PollTextBox.Text = ms.ToString();
    }
}