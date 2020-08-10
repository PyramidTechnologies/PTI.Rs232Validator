namespace PTI.Rs232Validator
{
    using System;
    using Messages;

    /// <summary>
    ///     Apex series RS232 implementation
    /// </summary>
    public class ApexValidator : BaseValidator
    {
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

            var nextMessage = _apexState.LastMessage ?? GetNextMasterMessage();

            var payload = nextMessage.Serialize();

            SerialProvider.Write(payload);

            // Device always responds with 11 bytes
            var deviceData = SerialProvider.Read(11);

            var pollResponse = new ApexResponseMessage(deviceData);

            if (!pollResponse.IsValid)
            {
                // Request retransmission
                _apexState.LastMessage = nextMessage;

                Logger?.Info("Invalid message: {0}", deviceData.ToHexString());
                Logger?.Info("Problems: {0}", string.Join(Environment.NewLine, pollResponse.PacketIssues));
                return;
            }

            // If ACK does not match, device is requesting a retransmit
            if (nextMessage.Ack != pollResponse.Ack)
            {
                // Request retransmission
                _apexState.LastMessage = nextMessage;

                return;
            }

            // Check for escrow features
            if (Config.IsEscrowMode)
            {
                // TODO handle escrow timeout
            }

            // Toggle the ACK, that was a successful poll
            _apexState.Ack = !_apexState.Ack;
            _apexState.LastMessage = null;

            // Clear the stack and return bits if set
            _apexState.StackNext = false;
            _apexState.ReturnNext = false;

            // Report on any state change
            if (_apexState.LastState != pollResponse.State)
            {
                var args = new StateChangeArgs(_apexState.LastState, pollResponse.State);
                StateChanged(args);

                _apexState.LastState = pollResponse.State;

                Logger?.Debug("State changed from {0} to {1}", args.OldState, args.NewState);
            }

            // Report on any active events
            if (pollResponse.Event != Rs232Event.None)
            {
                Logger?.Debug("Reporting event(s): {0}", pollResponse.Event);
                EventReported(pollResponse.Event);
            }

            // Report a missing cash box
            if (!pollResponse.IsCashBoxPresent)
            {
                if (!_apexState.CashBoxRemovalReported)
                {
                    Logger?.Debug("Reporting state: {0}", pollResponse.State);

                    _apexState.CashBoxRemovalReported = true;

                    CashBoxRemoved();
                }
            }
            else
            {
                // Clear the cash box removal flag so the next removal can raise and event
                _apexState.CashBoxRemovalReported = false;
            }

            // Report any available credit
            if (pollResponse.Credit.HasValue)
            {
                Logger?.Debug("Reporting credit: {0}", pollResponse.Credit);
                CreditReported(pollResponse.Credit.Value);
            }
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

            return nextMessage;
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
        ///     Timestamp of when escrow state was entered
        /// </summary>
        public DateTime EscrowStart;

        /// <summary>
        ///     Last polling message sent to device
        ///     Used for retransmission
        /// </summary>
        public Rs232BaseMessage LastMessage;
    }
}