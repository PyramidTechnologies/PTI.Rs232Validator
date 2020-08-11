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
                Logger?.Error("{0} Serial provider is not open", GetType().Name);
                return;
            }

            var pollResponse = SendPollMessage();

            if (pollResponse is null)
            {
                return;
            }


            HandleState(pollResponse);

            HandleEvents(pollResponse);

            HandleCashBox(pollResponse);

            HandleCredit(pollResponse);        
            
            // Handle escrow events last so logging and other events have a logical order
            HandleEscrow(pollResponse);

        }

        /// <inheritdoc />
        protected override void DoStack()
        {
            Logger?.Debug("{0} Issuing do-stack request", GetType().Name);
            _apexState.StackNext = true;
        }

        /// <inheritdoc />
        protected override void DoReturn()
        {
            Logger?.Debug("{0} Issuing do-return request", GetType().Name);
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
            
            // Log the parsed command and response together for easier analysis
            Logger?.Trace("{0} poll message: {1}", GetType().Name, nextMessage);
            Logger?.Trace("{0} poll response: {1}", GetType().Name, pollResponse);

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
                        Logger?.Debug("{0} Device is busy", GetType().Name);
                        return null;
                    }

                    Logger?.Error("{0} Device appears to be offline", GetType().Name);
                }

                // If there is a protocol violation and strict mode is enabled, 
                // report the problem and request a retransmit
                if (pollResponse.HasProtocolViolation && Config.StrictMode)
                {
                    Logger?.Error("{0} Invalid message: {1}", GetType().Name, deviceData.ToHexString());
                    Logger?.Error("{0} Problems: {1}", GetType().Name,
                        string.Join(Environment.NewLine, pollResponse.AllPacketIssues));
                    return null;
                }
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
            if (!pollResponse.CreditIndex.HasValue)
            {
                Logger?.Error("{0} Escrow state entered without a credit message", GetType().Name);
            }
            else
            {
                BillInEscrow(pollResponse.CreditIndex.Value);
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
            
            Logger?.Info("{0} Entering state: {1}", GetType().Name, pollResponse.State);

            _apexState.LastState = pollResponse.State;
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
            
            Logger?.Info("{0} Setting events(s): {1}", GetType().Name, pollResponse.Event);

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

                _apexState.CashBoxRemovalReported = true;
                
                Logger?.Info("{0} Reporting cash box removed", GetType().Name);

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

            if (!pollResponse.CreditIndex.HasValue)
            {
                Logger?.Error("{0} Stack event issued without a credit message", GetType().Name);
            }
            else
            {
                Logger?.Info("{0} Reporting credit index: {1}", GetType().Name, pollResponse.CreditIndex);
                CreditIndexReported(pollResponse.CreditIndex.Value);
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