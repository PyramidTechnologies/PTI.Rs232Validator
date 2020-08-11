namespace PTI.Rs232Validator.Emulator
{
    using System;
    using Providers;

    /// <summary>
    ///     Apex RS-232 mode emulator
    ///     This creates a serial provider that directly attaches to a
    ///     RS-232 state machine. The state of this instance can be directly modified
    ///     and the polling loop will automatically build the poll response based on
    ///     the configured state.
    /// </summary>
    public class ApexEmulator : ISerialProvider, IEmulator
    {
        private ApexDeviceMessage _nextResponse;
        private byte? _credit;

        /// <summary>
        ///     Create a new emulator in the PowerUp state
        /// </summary>
        public ApexEmulator()
        {
            CurrentState = Rs232State.None;
            CurrentEvents = Rs232Event.PowerUp;
            
            CashBoxPresent = true;
        }
        
        /// <inheritdoc />
        public event EventHandler OnPollResponseSent;

        /// <inheritdoc />
        public int TotalPollCount { get; private set; }

        /// <inheritdoc />
        public bool CashBoxPresent { get; set; }

        /// <inheritdoc />
        public Rs232State CurrentState { get; set; }

        /// <inheritdoc />
        public Rs232Event CurrentEvents { get; set; }

        /// <inheritdoc />
        public byte? Credit
        {
            get => _credit;
            set
            {
                if (value.HasValue)
                {
                    var v = value.Value;
                    if (v > 7)
                    {
                        throw new ArgumentException("Credit index must be in range (0,7)");
                    }
                }

                _credit = value;
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Fake port is always open
        /// </summary>
        public bool IsOpen => true;

        /// <inheritdoc />
        public ILogger Logger { get; set; }

        /// <summary>
        ///     Fake port can always be opened
        /// </summary>
        public bool TryOpen()
        {
            return true;
        }

        /// <inheritdoc />
        public void Close()
        {
            // Nothing to close
        }

        /// <inheritdoc />
        public byte[] Read(int count)
        {
            // Response is build when a host message is received via Write
            if (_nextResponse is null)
            {
                return null;
            }

            var payload = _nextResponse.Serialize();

            // Return no more than what's available or requested
            var readLen = Math.Min(count, payload.Length);

            var slice = new byte[readLen];
            Array.Copy(payload, slice, slice.Length);

            return slice;
        }

        /// <inheritdoc />
        public void Write(byte[] data)
        {
            // Handle polling request
            _nextResponse = PrepareNextResponse(data);

            // Handle post-processing
            ++TotalPollCount;
            OnPollResponseSent?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to do
        }

        /// <summary>
        ///    Parse host data to build a polling response
        /// </summary>
        /// <param name="dataFromHost">Message data received from host</param>
        private ApexDeviceMessage PrepareNextResponse(byte[] dataFromHost)
        {
            var response = new ApexDeviceMessage();

            var hostMessage = new ApexHostMessage(dataFromHost);

            // Malformed host message
            if (!hostMessage.IsHostMessage)
            {
                _nextResponse.SetEvents(Rs232Event.InvalidCommand);
                return response;
            }

            // Update the ACK state to match the host
            response.SetAck(hostMessage.Ack);

            // Report the current state
            response.SetState(CurrentState);

            // Report any events then clear them
            response.SetEvents(CurrentEvents);

            // Set the cash box presence
            response.SetCashBoxState(CashBoxPresent);

            // Set credit bits if specified
            if (Credit.HasValue)
            {
                response.SetCredit(Credit.Value);
                Credit = null;
            }

            return response;
        }
    }
}