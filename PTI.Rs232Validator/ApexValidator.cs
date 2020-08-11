namespace PTI.Rs232Validator
{
    using System;
    using System.Threading;
    using Messages;

    /// <summary>
    ///     Apex series RS232 implementation
    /// </summary>
    public class ApexValidator : BaseValidator
    {
        /// <summary>
        ///     When Apex is busy stacking it will not respond.
        ///     This sets the maximum threshold of messages to
        ///     treat as busy before we consider the acceptor offline.
        /// </summary>
        private const int MaxBusyMessages = 6;
        
        /// <summary>
        ///     Tracks Apex state between polling messages
        /// </summary>
        private ApexState _apexState;

        /// <summary>
        ///     Create a new Apex RS232 validator.
        ///     You must be using the RS232 Tx and Tx on the 18 pin IO.
        ///     You must have the acceptor configured for RS232 mode.
        /// </summary>
        /// <param name="config">Configuration options</param>
        public ApexValidator(Rs232Config config) : base(config)
        {
            _apexState = new ApexState();
        }

        /// <inheritdoc />
        protected override void PollDevice()
        {
            if (!SerialProvider.IsOpen)
            {
                Logger?.Error("Serial provider is not open");
                return;
            }

            var pollResponse = SendPollMessage();

            if (pollResponse is null)
            {
                return;
            }
            
            // Handle escrow event first so client has more time to take action
            HandleEscrow(pollResponse);

            HandleState(pollResponse);

            HandleEvents(pollResponse);

            HandleCashBox(pollResponse);

            HandleCredit(pollResponse);
        }

        /// <inheritdoc />
        protected override void DoStack()
        {
            Logger?.Debug("Issuing do-stack request");
            _apexState.StackNext = true;
        }

        /// <inheritdoc />
        protected override void DoReturn()
        {
            Logger?.Debug("Issuing do-return request");
            _apexState.ReturnNext = true;
        }

        /// <summary>
        ///     Send poll message to device and return a validated response.
        ///     If the response is malformed, null will be returned.
        /// </summary>
        /// <returns>Device response</returns>
        private ApexResponseMessage SendPollMessage()
        {
            // Replay the last message or build a new message
            var nextMessage = _apexState.LastMessage ?? GetNextMasterMessage();

            var payload = nextMessage.Serialize();

            SerialProvider.Write(payload);

            // Device always responds with 11 bytes
            var deviceData = TryPortRead();
            var pollResponse = new ApexResponseMessage(deviceData);

            // The response was invalid or incomplete
            if (!pollResponse.IsValid)
            {
                // Request retransmission (by not modifying the ACK and payload)
                _apexState.LastMessage = nextMessage;
                
                // If we suspect the validator is busy, try a few more times
                if (pollResponse.IsEmptyResponse && _apexState.BusyCount++ < MaxBusyMessages)
                {
                    if (_apexState.BusyCount++ < MaxBusyMessages)
                    {
                        Logger?.Info("Device is busy");
                        return null;
                    }
                    
                    Logger?.Error("Device appears to be offline");
                } 
                
                // Device is not busy, this is a bad response
                Logger?.Info("Invalid message: {0}", deviceData.ToHexString());
                Logger?.Info("Problems: {0}", string.Join(Environment.NewLine, pollResponse.PacketIssues));
                return null;
            }

            // If ACK does not match, then device is requesting a retransmit
            if (nextMessage.Ack != pollResponse.Ack)
            {
                // Request retransmission
                _apexState.LastMessage = nextMessage;

                return null;
            }

            // Toggle the ACK, that was a successful poll
            _apexState.Ack = !_apexState.Ack;
            _apexState.LastMessage = null;
            _apexState.BusyCount = 0;

            return pollResponse;
        }

        /// <summary>
        ///     Perform escrow actions
        /// </summary>
        /// <param name="pollResponse">Poll response from device</param>
        /// <remarks>If not in escrow mode no actions will be performed</remarks>
        private void HandleEscrow(Rs232ResponseMessage pollResponse)
        {
            if (!Config.IsEscrowMode || !pollResponse.State.HasFlag(Rs232State.Escrowed))
            {
                return;
            }

            // Handle escrow state
            if (!pollResponse.Credit.HasValue)
            {
                Logger?.Error("Escrow state entered without a credit message");
            }
            else
            {
                BillInEscrow(pollResponse.Credit.Value);
            }
        }

        /// <summary>
        ///     Handle state change actions
        /// </summary>
        /// <param name="pollResponse">Poll response from device</param>
        private void HandleState(Rs232ResponseMessage pollResponse)
        {
            // Report on any state change
            if (_apexState.LastState == pollResponse.State)
            {
                return;
            }

            var args = new StateChangeArgs(_apexState.LastState, pollResponse.State);
            StateChanged(args);

            _apexState.LastState = pollResponse.State;

            Logger?.Debug("State changed from {0} to {1}", args.OldState, args.NewState);
        }

        /// <summary>
        ///     Handle event notifications
        /// </summary>
        /// <param name="pollResponse">Poll response from device</param>
        private void HandleEvents(Rs232ResponseMessage pollResponse)
        {
            // Report on any active events
            if (pollResponse.Event == Rs232Event.None)
            {
                return;
            }

            Logger?.Debug("Reporting event(s): {0}", pollResponse.Event);
            EventReported(pollResponse.Event);
        }

        /// <summary>
        ///     Handle cash box notifications
        /// </summary>
        /// <param name="pollResponse">Poll response from device</param>
        private void HandleCashBox(Rs232ResponseMessage pollResponse)
        {
            // Report a missing cash box
            if (!pollResponse.IsCashBoxPresent)
            {
                if (_apexState.CashBoxRemovalReported)
                {
                    return;
                }

                Logger?.Debug("Reporting state: {0}", pollResponse.State);

                _apexState.CashBoxRemovalReported = true;

                CashBoxRemoved();
            }
            else
            {
                // Clear the cash box removal flag so the next removal can raise and event
                _apexState.CashBoxRemovalReported = false;
            }
        }

        /// <summary>
        ///     Handle credit notifications
        /// </summary>
        /// <param name="pollResponse">Poll response from device</param>
        private void HandleCredit(Rs232ResponseMessage pollResponse)
        {
            // Report any available credit
            if (!pollResponse.Event.HasFlag(Rs232Event.Stacked))
            {
                return;
            }

            if (!pollResponse.Credit.HasValue)
            {
                Logger?.Error("Stack event issued without a credit message");
            }
            else
            {
                Logger?.Debug("Reporting credit index: {0}", pollResponse.Credit);
                CreditIndexReported(pollResponse.Credit.Value);
            }
        }

        /// <summary>
        ///     Build the next message to send based on our current state
        /// </summary>
        /// <returns></returns>
        private Rs232BaseMessage GetNextMasterMessage()
        {
            var nextMessage = new Rs232PollMessage(_apexState.Ack)
                .SetEnableMask(Config.EnableMask)
                .SetStack(_apexState.StackNext)
                .SetReturn(_apexState.ReturnNext)
                .SetEscrowMode(Config.IsEscrowMode);

            // Clear the stack and return bits if set
            _apexState.StackNext = false;
            _apexState.ReturnNext = false;

            return nextMessage;
        }

        /// <summary>
        ///     Read from serial provider with retry
        /// </summary>
        /// <returns>Data from port or null on error</returns>
        private byte[] TryPortRead()
        {
            var backoff = TimeSpan.Zero;
            var retry = 3;
            byte[] deviceData = null;
            do
            {
                // Device always responds with 11 bytes
                deviceData = SerialProvider.Read(11);

                if (backoff != TimeSpan.Zero)
                {
                    Thread.Sleep(backoff);
                }

                backoff += TimeSpan.FromMilliseconds(50);

            } while (deviceData is null && retry-- > 0);

            return deviceData;
        }
    }

    /// <summary>
    ///     Holds all the state used between polling messages
    /// </summary>
    internal struct ApexState
    {
        /// <summary>
        ///     Bit is toggled on every successful message
        /// </summary>
        public bool Ack;

        /// <summary>
        ///     Last state reported by device
        /// </summary>
        public Rs232State LastState;

        /// <summary>
        ///     Don't spam the cash box removal event
        ///     When set, the event has already been raised
        ///     and the device has not reported the cash box
        ///     to have returned.
        /// </summary>
        public bool CashBoxRemovalReported;

        /// <summary>
        ///     When set, the stack flag will be set in the next polling message
        /// </summary>
        public bool StackNext;

        /// <summary>
        ///     When set, the return flag will be set in the next polling message
        /// </summary>
        public bool ReturnNext;

        /// <summary>
        ///     Last polling message sent to device
        ///     Used for retransmission
        /// </summary>
        public Rs232BaseMessage LastMessage;

        /// <summary>
        ///     Count of consecutive busy messages
        /// </summary>
        public int BusyCount;
    }
}