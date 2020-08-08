namespace PTI.Rs232Validator
{
    using System;
    using System.Threading;

    /// <summary>
    ///     Base implementation common to all validators
    /// </summary>
    public abstract class BaseValidator : IDisposable
    {
        private readonly object _mutex = new object();

        protected readonly Rs232Config Config;
        protected readonly ISerialProvider SerialProvider;
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

            Logger?.Info("Created new validator: {0}", config);
        }

        /// <summary>
        ///     Optional logger to attach to this acceptor
        /// </summary>
        protected ILogger Logger { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            SerialProvider?.Dispose();

            Logger?.Debug("Validator disposed");
        }

        /// <summary>
        ///     Raised when the state of the bill acceptor changes
        /// </summary>
        public event EventHandler<StateChangeArgs> StateChanged;

        /// <summary>
        ///     Raised when one ore more events are reported by the device
        /// </summary>
        public event EventHandler<Rs232Event> EventReported;

        /// <summary>
        ///     Raised when credit is reported. The reported
        ///     value is the RS232 credit index.
        /// </summary>
        public event EventHandler<int> CreditReported;

        /// <summary>
        ///     Raised when the cash box is removed from validator
        /// </summary>
        public event EventHandler CashBoxRemoved;

        /// <summary>
        ///     Attempt to start the RS232 polling loop
        /// </summary>
        /// <returns>True when loop starts</returns>
        public bool StartPollingLoop()
        {
            return StartPollingLoop(CancellationToken.None);
        }

        /// <summary>
        ///     Attempt to start the RS232 polling loop
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns>True when loop starts</returns>
        public bool StartPollingLoop(CancellationToken token)
        {
            lock (_mutex)
            {
                if (_isRunning)
                {
                    Logger?.Error("Already polling, ignoring start request");
                    return false;
                }

                if (!SerialProvider.TryOpen())
                {
                    Logger?.Error("Failed to open serial provider");
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

            Logger?.Info($"Polling thread started: {_rs232Worker.ManagedThreadId}");

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
                    Logger?.Error("Polling loop is not running, ignoring stop command");
                    return;
                }

                _isRunning = false;
            }

            Logger?.Info("Stopping polling loop...");

            if (!_rs232Worker.Join(TimeSpan.FromSeconds(10)))
            {
                Logger?.Error("Failed to stop polling loop");
            }
            else
            {
                Logger?.Info("Polling loop stopped");
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
                Logger.Error("Cannot manually issue stack command in non-escrow mode");
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
                Logger.Error("Cannot manually issue return command in non-escrow mode");
                return;
            }

            DoReturn();
        }

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
                        Logger?.Debug("MainLoop received stop signal");
                        break;
                    }
                }

                PollDevice();

                Thread.Sleep(Config.PollingPeriod);
            }
        }

        /// <summary>
        ///     Send next polling message and parse the response
        ///     This will trigger events for State, Event, and Credit messages
        /// </summary>
        protected abstract void PollDevice();

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
        protected void OnStateChanged(StateChangeArgs args)
        {
            StateChanged?.Invoke(this, args);
        }

        /// <summary>
        ///     Raise the event reported event
        /// </summary>
        protected void OnEventReported(Rs232Event evt)
        {
            EventReported?.Invoke(this, evt);
        }

        /// <summary>
        ///     Raise the credit event
        /// </summary>
        protected void OnCreditReported(int value)
        {
            CreditReported?.Invoke(this, value);
        }

        /// <summary>
        ///     Raise cash box removed event
        /// </summary>
        protected void OnCashBoxRemoved()
        {
            CashBoxRemoved?.Invoke(this, EventArgs.Empty);
        }
    }
}