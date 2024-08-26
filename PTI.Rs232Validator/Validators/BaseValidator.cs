using PTI.Rs232Validator.Loggers;
using PTI.Rs232Validator.Providers;
using System;
using System.Threading;

namespace PTI.Rs232Validator.Validators;

/// <summary>
///     Base implementation common to all validators
/// </summary>
public abstract class BaseValidator : IDisposable
{
    private readonly object _mutex = new();

    /// <summary>
    ///     Serial provider instance
    /// </summary>
    protected readonly ISerialProvider SerialProvider;

    /// <summary>
    ///     Event is triggered after a number of polling
    ///     cycles to assert that the device is operating
    ///     normally.
    /// </summary>
    private readonly CounterEvent _deviceIsReady;

    private bool _isRunning;
    private Thread _rs232Worker;

    /// <summary>
    ///     Create a new base validator
    /// </summary>
    /// <param name="config">Configuration to use</param>
    protected BaseValidator(Rs232Config config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        SerialProvider = config.SerialProvider;

        // All services will use this logger instance
        Config.Logger ??= new NullLogger();
        SerialProvider.Logger = Config.Logger;
        Logger = Config.Logger;

        // Wait for this many polls before saying the acceptor is online
        _deviceIsReady = new CounterEvent(2);

        Logger?.Info("{0} Created new validator: {1}", GetType().Name, config);
    }

    /// <summary>
    ///     Gets the active RS-232 configuration
    ///     You cannot change the configuration of a running
    ///     validator.
    /// </summary>
    public Rs232Config Config { get; }

    /// <summary>
    ///     Optional logger to attach to this acceptor
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        SerialProvider.Dispose();
        Logger.Trace("{0} disposed", GetType().Name);
    }

    /// <summary>
    ///     Raised when the state of the bill acceptor changes
    /// </summary>
    public event EventHandler<StateChangeArgs> OnStateChanged;

    /// <summary>
    ///     Raised when one ore more events are reported by the device
    /// </summary>
    public event EventHandler<Rs232Event> OnEventReported;

    /// <summary>
    ///     Raised when credit is reported. The reported
    ///     value is the RS232 credit index.
    /// </summary>
    public event EventHandler<int> OnCreditIndexReported;

    /// <summary>
    ///     Raised when a bill is being help in escrow.
    ///     The reported value is the RS232 credit index.
    ///     This event is raised while the device is holding
    ///     the bill in escrow. In other words, this event
    ///     may be raised multiple times. Use the event
    ///     <see cref="OnCreditIndexReported" /> event to obtain
    ///     the final credit-issue notification.
    /// </summary>
    /// <remarks>Only raised in escrow mode</remarks>
    public event EventHandler<int> OnBillInEscrow;

    /// <summary>
    ///     Raised when the cash box is removed from validator.
    ///     You may also poll <see cref="IsCashBoxPresent"/>.
    /// </summary>
    public event EventHandler OnCashBoxRemoved;

    /// <summary>
    ///     Raised when the cash box is attached to the validator.
    ///     You may also poll <see cref="IsCashBoxPresent"/>.
    ///     This event will only be raised once the cash box
    ///     has been reported as missing for the first time.
    ///     Otherwise, you would see this event at every startup.
    /// </summary>
    public event EventHandler OnCashBoxAttached;

    /// <summary>
    ///     Raised when the API suspects the device connection has
    ///     been lost. You can use this to be notified of a lost
    ///     connection or you can poll <see cref="IsUnresponsive"/>.
    /// </summary>
    public event EventHandler OnLostConnection;

    /// <summary>
    ///     Attempt to start the RS232 polling loop
    /// </summary>
    /// <returns>True when loop starts</returns>
    public bool StartPollingLoop()
    {
        lock (_mutex)
        {
            if (_isRunning)
            {
                Logger.Error("{0} Already polling, ignoring start request", GetType().Name);
                return false;
            }

            if (!SerialProvider.TryOpen())
            {
                Logger.Error("{0} Failed to open serial provider", GetType().Name);
                return false;
            }

            _isRunning = true;
        }

        _rs232Worker = new Thread(MainLoop)
        {
            // Terminate if our parent thread dies
            IsBackground = true
        };

        _rs232Worker.Start();

        if (Config.DisableLivenessCheck)
        {
            Logger.Info("{0} Polling thread started (no liveness check): {1}", GetType().Name,
                _rs232Worker.ManagedThreadId);
        }
        else
        {
            // RS-232 does not have a "ping" concept so instead we wait for a 
            // number of healthy messages before telling the caller that the 
            // message loop has "started successfully".
            if (!_deviceIsReady.WaitOne(Extensions.Multiply(Config.PollingPeriod, 5)))
            {
                Logger.Info("{0} timed out waiting for a valid polling response", GetType().Name);
                return false;
            }

            Logger.Info("{0} Polling thread started: {1}", GetType().Name, _rs232Worker.ManagedThreadId);
        }

        return true;
    }

    /// <summary>
    ///     Stop the RS232 polling loop
    /// </summary>
    public void StopPollingLoop()
    {
        lock (_mutex)
        {
            if (!_isRunning)
            {
                Logger.Error("{0} Polling loop is not running, ignoring stop command", GetType().Name);
                return;
            }

            _isRunning = false;
        }

        Logger.Debug("{0} Stopping polling loop...", GetType().Name);

        if (!_rs232Worker.Join(TimeSpan.FromSeconds(10)))
        {
            Logger.Error("{0} Failed to stop polling loop", GetType().Name);
        }
        else
        {
            Logger.Info("{0} Polling loop stopped", GetType().Name);
        }

        try
        {
            SerialProvider.Close();
        }
        catch (Exception ex)
        {
            Logger?.Error("{0} Unable to close serial provider: {1}", GetType().Name, ex.Message);
        }
    }

    /// <summary>
    ///     Perform escrow stack
    /// </summary>
    /// <remarks>Configuration must specify Escrow Mode for this to work</remarks>
    public void Stack()
    {
        if (!Config.IsEscrowMode)
        {
            Logger.Error("{0} Cannot manually issue stack command in non-escrow mode", GetType().Name);
            return;
        }

        DoStack();
    }

    /// <summary>
    ///     Perform escrow return
    /// </summary>
    /// <remarks>Configuration must specify Escrow Mode for this to work</remarks>
    public void Return()
    {
        if (!Config.IsEscrowMode)
        {
            Logger.Error("{0} Cannot manually issue return command in non-escrow mode", GetType().Name);
            return;
        }

        DoReturn();
    }

    /// <summary>
    ///     Disables the bill acceptor within the time period defined by the poll rate.
    ///     The poll rate is the maximum time between poll packets from host to device.
    ///     This tells the acceptor to stop accepting bills but keep reporting status.
    ///     The acceptor's lights will turn off after this call takes effect.
    ///     <seealso cref="ResumeAcceptance" />
    /// </summary>
    /// <remarks>The command will take up to <see cref="Rs232Config.PollingPeriod"/> to take effect.</remarks>
    public abstract void PauseAcceptance();

    /// <summary>
    ///     Returns the acceptor to bill accepting mode.
    ///     This command has no effect if the acceptor is already running and accepting.
    ///     The acceptor's lights will turn on after this command takes effect.
    ///     <seealso cref="PauseAcceptance" />
    /// </summary>
    /// <remarks>The command will take up to <see cref="Rs232Config.PollingPeriod"/> to take effect.</remarks> 
    public abstract void ResumeAcceptance();

    /// <summary>
    ///     Returns true if acceptance is current paused
    ///     <seealso cref="PauseAcceptance" />
    /// </summary>
    public abstract bool IsPaused { get; }

    /// <summary>
    ///     Returns true if the API thinks the device has stopped responding
    /// </summary>
    public abstract bool IsUnresponsive { get; }

    /// <summary>
    ///     Returns true if cash box is present.
    ///     <see cref="OnCashBoxRemoved"/> will notify you
    ///     if the cash box is removed.
    /// </summary>
    public bool IsCashBoxPresent { get; protected set; }

    /// <summary>
    ///     Main loop thread
    /// </summary>
    private void MainLoop()
    {
        while (true)
        {
            lock (_mutex)
            {
                if (!_isRunning)
                {
                    Logger.Debug("{0} MainLoop received stop signal", GetType().Name);
                    break;
                }
            }

            if (PollDevice())
            {
                _deviceIsReady.Set();
            }

            Thread.Sleep(Config.PollingPeriod);
        }
    }

    /// <summary>
    ///     Send next polling message and parse the response
    ///     This will trigger events for State, Event, and Credit messages
    /// </summary>
    /// <returns>true if polling loop transmits and receives without fault</returns>
    protected abstract bool PollDevice();

    /// <summary>
    ///     Perform escrow stack function
    /// </summary>
    protected abstract void DoStack();

    /// <summary>
    ///     Perform escrow return function
    /// </summary>
    protected abstract void DoReturn();

    /// <summary>
    ///     Raise the state change event
    /// </summary>
    protected void StateChanged(StateChangeArgs args)
    {
        OnStateChanged.Invoke(this, args);
    }

    /// <summary>
    ///     Raise the event reported event
    /// </summary>
    protected void EventReported(Rs232Event evt)
    {
        OnEventReported.Invoke(this, evt);
    }

    /// <summary>
    ///     Raise the credit event
    /// </summary>
    /// <param name="index">Bill index</param>
    protected void CreditIndexReported(int index)
    {
        OnCreditIndexReported.Invoke(this, index);
    }

    /// <summary>
    ///     Raise the bill in escrow event
    /// </summary>
    /// <param name="index">Bill index</param>
    protected void BillInEscrow(int index)
    {
        OnBillInEscrow.Invoke(this, index);
    }

    /// <summary>
    ///     Raise cash box removed event
    /// </summary>
    protected void CashBoxRemoved()
    {
        OnCashBoxRemoved.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Raise cash box attached event
    /// </summary>
    protected void CashBoxAttached()
    {
        OnCashBoxAttached.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Raise the lost connection event
    /// </summary>
    protected void LostConnection()
    {
        OnLostConnection.Invoke(this, EventArgs.Empty);
    }
}