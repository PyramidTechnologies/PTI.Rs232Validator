namespace PTI.Rs232Validator.Tests.Emulator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Messages;
    using Providers;

    public class ApexEmulator : ISerialProvider
    {
        private readonly Stack<Tuple<Rs232State, Rs232Event>> _sequencer =
            new Stack<Tuple<Rs232State, Rs232Event>>();

        private int? _nextCredit;
        private ApexDeviceMessage _nextResponse;

        public ApexEmulator()
        {
            AddSequence(Rs232State.Idling, Rs232Event.PowerUp);
            CashBoxPresent = true;
        }

        public int TotalPollCount { get; private set; }

        /// <summary>
        ///     When true, the cash box is reported as present
        /// </summary>
        public bool CashBoxPresent { get; set; }

        public Rs232State CurrentState { get; set; }

        public Rs232Event CurrentEvents { get; set; }

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
            PrepareNextResponse(data);

            ++TotalPollCount;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to do
        }

        public void AddSequence(Rs232State state, Rs232Event evt)
        {
            _sequencer.Push(new Tuple<Rs232State, Rs232Event>(state, evt));
        }

        public void ReportCredit(int billNumber)
        {
            if (billNumber < 0 || billNumber > 7)
            {
                throw new ArgumentException($"Invalid bill number: {billNumber}. Must be in range (0,7)");
            }

            // Accepting state
            _sequencer.Push(new Tuple<Rs232State, Rs232Event>(
                Rs232State.Accepting, Rs232Event.None));

            // Stacking
            _sequencer.Push(new Tuple<Rs232State, Rs232Event>(
                Rs232State.Stacking, Rs232Event.None));

            // Idle + Stacked
            _sequencer.Push(new Tuple<Rs232State, Rs232Event>(
                Rs232State.Idling, Rs232Event.Stacked));

            // Capture credit to report
            _nextCredit = billNumber;
        }

        private (Rs232State, Rs232Event) GetNextSequence()
        {
            if (_sequencer.Count == 0)
            {
                AddSequence(CurrentState, CurrentEvents);

                CurrentEvents = Rs232Event.None;
            }

            var (state, evt) = _sequencer.Pop();

            CurrentState = state;

            return (state, evt);
        }

        private void PrepareNextResponse(byte[] data)
        {
            _nextResponse = new ApexDeviceMessage();

            var hostMessage = new ApexHostMessage(data);

            // Malformed host message
            if (!hostMessage.IsHostMessage)
            {
                _nextResponse.SetEvents(Rs232Event.InvalidCommand);
                return;
            }

            var (state, evt) = GetNextSequence();

            // Update the ACK state to match the host
            _nextResponse.SetAck(hostMessage.Ack);

            // Report the current state
            _nextResponse.SetState(state);

            // Report any events then clear them
            _nextResponse.SetEvents(evt);

            // Set the cashbox presence
            _nextResponse.SetCashBoxState(CashBoxPresent);

            if (_nextCredit.HasValue)
            {
                _nextResponse.SetCredit(_nextCredit.Value);
                _nextCredit = null;
            }
        }
    }

    internal class ApexHostMessage : Rs232BaseMessage
    {
        public ApexHostMessage(byte[] data) : base(data)
        {
        }
    }

    internal class ApexDeviceMessage : Rs232BaseMessage
    {
        private const int AckByte = 2;
        private const int CashBoxByte = 4;
        private const int CashBoxBit = 4;
        private const int CreditByte = 5;

        /// <summary>
        ///     Standard device message
        /// </summary>
        private static readonly byte[] BaseMessage =
        {
            0x02, 0x0B, 0x20, 0x00, 0x00, 0x00, 0x00, 0x12, 0x13, 0x03, 0x3B
        };

        /// <summary>
        ///     State map keys by (byte, bit) into the payload.
        ///     e.g.
        ///     (1,2) => byte 1 bit 2 of payload
        /// </summary>
        private static readonly Dictionary<Rs232State, Tuple<byte, byte>> StateMap =
            new Dictionary<Rs232State, Tuple<byte, byte>>
            {
                {Rs232State.None, null},
                {Rs232State.Idling, new Tuple<byte, byte>(3, 0)},
                {Rs232State.Accepting, new Tuple<byte, byte>(3, 1)},
                {Rs232State.Escrowed, new Tuple<byte, byte>(3, 2)},
                {Rs232State.Stacking, new Tuple<byte, byte>(3, 3)},
                {Rs232State.Returning, new Tuple<byte, byte>(3, 5)},
                {Rs232State.BillJammed, new Tuple<byte, byte>(4, 2)},
                {Rs232State.StackerFull, new Tuple<byte, byte>(4, 3)},
                {Rs232State.Failure, new Tuple<byte, byte>(5, 2)}
            };

        /// <summary>
        ///     Event map keys by (byte, bit) into the payload.
        ///     e.g.
        ///     (1,2) => byte 1 bit 2 of payload
        /// </summary>
        private static readonly Dictionary<Rs232Event, Tuple<byte, byte>> EventMap =
            new Dictionary<Rs232Event, Tuple<byte, byte>>
            {
                {Rs232Event.Stacked, new Tuple<byte, byte>(3, 4)},
                {Rs232Event.Returned, new Tuple<byte, byte>(3, 6)},
                {Rs232Event.Cheated, new Tuple<byte, byte>(4, 0)},
                {Rs232Event.BillRejected, new Tuple<byte, byte>(4, 1)},
                {Rs232Event.PowerUp, new Tuple<byte, byte>(5, 0)},
                {Rs232Event.InvalidCommand, new Tuple<byte, byte>(5, 1)}
            };

        public ApexDeviceMessage() : base(BaseMessage)
        {
            RawMessage[^1] = CalculateChecksum();
        }

        public ApexDeviceMessage SetAck(bool ack)
        {
            RawMessage[AckByte] = (byte) (ack ? 0x21 : 0x20);
            RawMessage[^1] = CalculateChecksum();
            return this;
        }

        public ApexDeviceMessage SetState(Rs232State state)
        {
            if (state == Rs232State.None)
            {
                return this;
            }

            var (index, bit) = StateMap[state];

            RawMessage[index] = SetBit(bit, RawMessage[index]);
            RawMessage[^1] = CalculateChecksum();

            return this;
        }

        public ApexDeviceMessage SetEvents(Rs232Event events)
        {
            foreach (var evt in Enum.GetValues(typeof(Rs232Event)).Cast<Rs232Event>())
            {
                if (!events.HasFlag(evt) || evt == Rs232Event.None)
                {
                    continue;
                }

                var (index, bit) = EventMap[evt];

                RawMessage[index] = SetBit(bit, RawMessage[index]);
            }

            RawMessage[^1] = CalculateChecksum();

            return this;
        }

        public ApexDeviceMessage SetCashBoxState(bool present)
        {
            RawMessage[CashBoxByte] = present
                ? SetBit(CashBoxBit, RawMessage[CashBoxByte])
                : ClearBit(CashBoxBit, RawMessage[CashBoxByte]);

            RawMessage[^1] = CalculateChecksum();

            return this;
        }

        public ApexDeviceMessage SetCredit(int credit)
        {
            if (credit < 0 || credit > 7)
            {
                throw new ArgumentException($"Invalid credit value: {nameof(credit)}. Must in range (0,7).");
            }

            credit = (credit << 3) & 0b00111000;
            RawMessage[CreditByte] = (byte) credit;

            RawMessage[^1] = CalculateChecksum();

            return this;
        }
    }
}