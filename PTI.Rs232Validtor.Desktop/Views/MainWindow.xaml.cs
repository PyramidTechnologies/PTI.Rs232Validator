using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.SerialProviders;
using PTI.Rs232Validator.Validators;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PTI.Rs232Validator.Desktop.Views;

// This portion implements INotifyPropertyChanged and establishes a connection to an acceptor.
/// <summary>
/// Main window of application.
/// </summary>
public partial class MainWindow : INotifyPropertyChanged
{
    private readonly string _selectPortText;
    private readonly string _connectText;
    private readonly string _disconnectText;
    private readonly string _pauseText;
    private readonly string _resumeText;
    private readonly string _stateTagText;
    private readonly string _eventTagText;

    private string _portName = string.Empty;
    private bool _isConnected;

    public MainWindow()
    {
        InitializeComponent();

        _selectPortText = FindResource("SelectPortText") as string ??
                          throw new InvalidOperationException("String resource with key 'SelectPortText' not found.");
        _connectText = FindResource("ConnectText") as string ??
                       throw new InvalidOperationException("String resource with key 'ConnectText' not found.");
        _disconnectText = FindResource("DisconnectText") as string ??
                          throw new InvalidOperationException("String resource with key 'DisconnectText' not found.");
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
    /// Is there an established connection to an acceptor?
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            DoOnUiThread(() => { ConnectButton.Content = value ? _disconnectText : _connectText; });

            _isConnected = value;
            NotifyPropertyChanged(nameof(IsConnected));
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

    private void AvailablePorts_Loaded(object sender, RoutedEventArgs e)
    {
        AvailablePortsComboBox.ItemsSource = SerialPort.GetPortNames();
    }
    
    private void AvailablePorts_MouseLeave(object sender, MouseEventArgs e)
    {
        AvailablePortsComboBox.ItemsSource = SerialPort.GetPortNames();
    }
    
    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        BillValidator?.StopPollingLoop();
        BillValidator?.Dispose();
        
        if (IsConnected)
        {
            IsConnected = false;
            ConnectButton.Content = _connectText;
            return;
        }

        if (string.IsNullOrEmpty(AvailablePortsComboBox.Text) || AvailablePortsComboBox.Text == _selectPortText)
        {
            MessageBox.Show("Please select a port.");
            return;
        }

        _portName = AvailablePortsComboBox.Text;

        var logger = new NullLogger();
        var serialPortProvider = SerialPortProvider.CreateUsbSerialProvider(logger, _portName);
        if (serialPortProvider is null)
        {
            MessageBox.Show("Failed to connect to port.");
            return;
        }

        var rs232Configuration = new Rs232Configuration();
        BillValidator = new BillValidator(logger, serialPortProvider, rs232Configuration);
        
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

        // Start the RS-232 polling loop.
        IsConnected = BillValidator.StartPollingLoop();
        if (!IsConnected)
        {
            BillValidator?.Dispose();
            MessageBox.Show("Failed to connect to the acceptor.");
        }
    }
    
    private void BillValidator_OnConnectionLost(object? sender, EventArgs e)
    {
        LogInfo("Lost connection to the acceptor.");
        IsConnected = false;
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
    /// Resets the connection to <see cref="BillValidator"/>.
    /// </summary>
    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (BillValidator is null)
        {
            return;
        }
        
        BillValidator.StopPollingLoop();
        await Task.Delay(BillValidator.Configuration.PollingPeriod);

        IsConnected = BillValidator.StartPollingLoop();
        if (!IsConnected)
        {
            MessageBox.Show("Failed to reset a connection to the acceptor.");
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